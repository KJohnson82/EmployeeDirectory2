# deploy.ps1

# Step 1: Generate docker-compose.yaml with placeholder env vars
aspire publish

# Step 2: Build images, set the deploy tag, generate .env.Production
# with filled registry image names, cache deployment state
aspire do prepare-employeedirectory-env --environment Production

# Step 3 
aspire deploy --environment Production

# Step 3: Push the built images to the private registry
# (uses the same cached deploy tag from step 2)
aspire do push --environment Production

# Step 4: Deploy to remote server
docker --context proxmox-lxc compose `
    -p employeedirectory `
    -f .\EmployeeDirectory.AppHost\aspire-output\docker-compose.yaml `
    --env-file .\EmployeeDirectory.AppHost\aspire-output\.env.Production `
    up -d --remove-orphans --pull always