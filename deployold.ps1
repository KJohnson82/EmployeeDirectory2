<#
.SYNOPSIS
Automated deployment for EmployeeDirectory (.NET 10 Aspire) to Proxmox LXC.
#>

$ErrorActionPreference = "Stop"

# --- Configuration ---
$SolutionPath = "D:\Software_Development\EmployeeDirectory\EmployeeDirectory"
$AppHostPath = "$SolutionPath\EmployeeDirectory.AppHost"
$AspireOutputDir = "$AppHostPath\aspire-output"
$Registry = "10.169.176.246:5000"
$Environment = "Production"
$DockerContext = "proxmox-lxc"

# --- Helper Function for External Commands ---
function Assert-Success {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ ERROR: The last command failed with exit code $LASTEXITCODE. Aborting deployment." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host "Starting deployment pipeline for EmployeeDirectory..." -ForegroundColor Cyan

# --- Pre-flight Checks ---
Write-Host "`n[0/5] Running pre-flight checks..." -ForegroundColor Yellow

# Check if Docker Desktop is running
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Docker Desktop is not running. Start it first." -ForegroundColor Red
    exit 1
}
Write-Host "  [OK] Docker Desktop is running" -ForegroundColor Green

# Check if the Proxmox context exists
$contexts = docker context ls --format "{{.Name}}"
if ($contexts -notcontains $DockerContext) {
    Write-Host "❌ ERROR: Docker context '$DockerContext' not found. Run 'docker context create' first." -ForegroundColor Red
    exit 1
}
Write-Host "  [OK] Docker context '$DockerContext' exists" -ForegroundColor Green

# Check if registry is reachable
try {
    Invoke-RestMethod -Uri "http://$Registry/v2/_catalog" -TimeoutSec 5 | Out-Null
    Write-Host "  [OK] Registry at $Registry is reachable" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR: Cannot reach registry at http://$Registry" -ForegroundColor Red
    Write-Host "  Make sure the registry container is running on the server." -ForegroundColor Yellow
    exit 1
}

# Safely change directory (guarantees we return to the original folder if script fails)
Push-Location -Path $AppHostPath
try {
    # 1. Generate Assets & Build Images
    Write-Host "`n[1/5] Generating Aspire deployment assets & Building Images..." -ForegroundColor Yellow
    aspire publish
    Assert-Success
    aspire do prepare-employeedirectory-env --environment $Environment
    Assert-Success

    # 2. Navigate to Output Directory
    Set-Location -Path $AspireOutputDir

    # 3. Smart-Tagging
    Write-Host "`n[2/5] Hunting down and tagging Aspire images..." -ForegroundColor Yellow
    $apiLocal = (docker images "ed-api" --format "{{.Repository}}:{{.Tag}}")[0]
    $adminLocal = (docker images "ed-admin" --format "{{.Repository}}:{{.Tag}}")[0]

    if (-not $apiLocal -or -not $adminLocal) {
        Write-Host "❌ ERROR: Could not find the built images in Docker. Did Aspire fail to build them?" -ForegroundColor Red
        exit 1
    }

    Write-Host "  Found API:   $apiLocal" -ForegroundColor DarkGray
    Write-Host "  Found Admin: $adminLocal" -ForegroundColor DarkGray

    docker tag $apiLocal "$Registry/ed-api:latest"
    docker tag $adminLocal "$Registry/ed-admin:latest"

    # 4. Push to Registry
    Write-Host "`n[3/5] Pushing images to Proxmox Registry ($Registry)..." -ForegroundColor Yellow
    docker push "$Registry/ed-api:latest"
    Assert-Success
    docker push "$Registry/ed-admin:latest"
    Assert-Success

    # 5. Patch .env and docker-compose.yaml for production
    Write-Host "`n[4/5] Patching deployment files for production..." -ForegroundColor Yellow

    # --- Patch .env ---
    # IMPORTANT: Keep ports as internal-only values (just "8080") so that
    # YARP's service discovery variables resolve correctly.
    # The gateway handles all external routing on port 80.
    Write-Host "  Patching .env.$Environment..." -ForegroundColor DarkGray
    $envContent = Get-Content ".env.$Environment"
    $envContent = $envContent -replace '^ED_API_IMAGE=.*', "ED_API_IMAGE=$Registry/ed-api:latest"
    $envContent = $envContent -replace '^ED_ADMIN_IMAGE=.*', "ED_ADMIN_IMAGE=$Registry/ed-admin:latest"
    $envContent = $envContent -replace '^ED_API_PORT=.*', "ED_API_PORT=8080"
    $envContent = $envContent -replace '^ED_ADMIN_PORT=.*', "ED_ADMIN_PORT=8080"
    $envContent | Set-Content ".env.$Environment"

    # --- Patch docker-compose.yaml ---
Write-Host "  Patching docker-compose.yaml..." -ForegroundColor DarkGray
$yamlPath = "docker-compose.yaml"
$yamlText = Get-Content $yamlPath -Raw

# Fix 1: YARP gateway — change expose 5000 to ports 80:5000
$yamlText = $yamlText -replace '(ed-gateway:[\s\S]*?)expose:\s*\n\s*- "5000"', '$1ports:
      - "80:5000"'

# Fix 2: ed-api — change ports to expose (internal only)
$yamlText = $yamlText -replace '(ed-api:[\s\S]*?)ports:\s*\n\s*- "\$\{ED_API_PORT\}"', '$1expose:
      - "8080"'

# Fix 3: ed-admin — change ports to expose (internal only)
$yamlText = $yamlText -replace '(ed-admin:[\s\S]*?)ports:\s*\n\s*- "\$\{ED_ADMIN_PORT\}"', '$1expose:
      - "8080"'

# Fix 4: Add missing ed-admin cluster destination
if ($yamlText -notmatch 'cluster_ed-admin__DESTINATIONS') {
    $yamlText = $yamlText -replace `
        '(REVERSEPROXY__CLUSTERS__cluster_ed-api__DESTINATIONS__destination1__ADDRESS:\s*"[^"]*")', `
        "`$1`n      REVERSEPROXY__CLUSTERS__cluster_ed-admin__DESTINATIONS__destination1__ADDRESS: `"https+http://ed-admin`""
}

# Fix 5: Add restart policy ONLY to services that don't already have one
$services = @('ed-postgres:', 'ed-redis:', 'ed-api:', 'ed-admin:', 'ed-gateway:')
foreach ($svc in $services) {
    # Only add restart if this service block doesn't already contain "restart:"
    if ($yamlText -match "(?ms)  $svc.*?(?=\n  [a-z]|\nnetworks:|\nvolumes:|\z)" ) {
        $block = $Matches[0]
        if ($block -notmatch 'restart:') {
            $yamlText = $yamlText -replace "(  $svc)", "`$1`n    restart: unless-stopped"
        }
    }
}

# Fix 6: Add depends_on for gateway
if ($yamlText -match 'ed-gateway:' -and $yamlText -notmatch 'ed-gateway:[\s\S]*?depends_on:') {
    $yamlText = $yamlText -replace `
        '(ed-gateway:)', `
        "`$1`n    depends_on:`n      ed-api:`n        condition: `"service_started`"`n      ed-admin:`n        condition: `"service_started`""
}

$yamlText | Set-Content $yamlPath -NoNewline
Write-Host "  [OK] Compose file patched" -ForegroundColor Green

    # Pull latest images
    docker --context $DockerContext compose -p employeedirectory -f docker-compose.yaml --env-file ".env.$Environment" pull
    Assert-Success

    # Deploy with orphan cleanup
    docker --context $DockerContext compose -p employeedirectory -f docker-compose.yaml --env-file ".env.$Environment" up -d --remove-orphans
    Assert-Success

    # Quick health check
    Write-Host "`nWaiting 15 seconds for containers to start..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 15

    Write-Host "Checking gateway health..." -ForegroundColor DarkGray
    try {
        $health = Invoke-WebRequest -Uri "http://10.169.176.246/health" -TimeoutSec 10 -UseBasicParsing
        Write-Host "  [OK] Gateway responded: $($health.Content)" -ForegroundColor Green
    } catch {
        Write-Host "  [WARN] Gateway health check failed (may still be starting)" -ForegroundColor Yellow
        Write-Host "  Try manually: curl http://10.169.176.246/health" -ForegroundColor Yellow
    }

    Write-Host "`nDeployment Complete! 🚀" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  App (via YARP):  http://10.169.176.246/" -ForegroundColor White
    Write-Host "  Health Check:    http://10.169.176.246/health" -ForegroundColor White
    Write-Host "  API Sync:        http://10.169.176.246/api/directory/sync" -ForegroundColor White
    Write-Host "  pgAdmin:         http://10.169.176.246:5050" -ForegroundColor White
    Write-Host "  Aspire Dashboard: http://10.169.176.246:7070" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan
}
finally {
    # Always return the terminal to where the user started
    Pop-Location
}