var builder = DistributedApplication.CreateBuilder(args);

// ===== CONFIGURATION FLAGS =====
var isPublishMode = builder.ExecutionContext.IsPublishMode;
var enableNginx = false; // Flip to true when ready for reverse proxy

// ===== DOCKER COMPOSE ENVIRONMENT (for publish/deploy) =====

    builder.AddDockerComposeEnvironment("employeedirectory-env")
        .WithProperties(env =>
        {
            env.DashboardEnabled = true;
        })
        .WithDashboard(dashboard =>
        {
            dashboard.WithHostPort(7070)
            .PublishAsDockerComposeService((resource, service) =>
            {
                service.Name = "ed-env";
            })
                .WithForwardedHeaders(enabled: true);
        });


// ===== DATABASE =====
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("ed-postgres", password: postgresPassword)
    .WithDataVolume("employeedirectory-pgdata")
    .WithLifetime(isPublishMode ? ContainerLifetime.Persistent : ContainerLifetime.Session)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-postgres";
    })
    .WithPgAdmin();

var employeeDirectoryDb = postgres.AddDatabase("employeedirectory-db");

// ===== CACHE =====
var redis = builder.AddRedis("ed-redis")
    .WithDataVolume("employeedirectory-redis-data")
    .WithLifetime(isPublishMode ? ContainerLifetime.Persistent : ContainerLifetime.Session)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-redis";
    });

// ===== API =====
var edapi = builder.AddProject<Projects.EmployeeDirectory_API>("ed-api")
    .WithReference(employeeDirectoryDb)
    .WaitFor(employeeDirectoryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-api";
    });

// ===== ADMIN =====
var admin = builder.AddProject<Projects.EmployeeDirectory_Admin>("ed-admin")
    .WithReference(employeeDirectoryDb)
    .WaitFor(employeeDirectoryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-admin";
    });

// ===== NGINX REVERSE PROXY (Disabled — flip enableNginx when ready) =====
if (enableNginx && isPublishMode)
{
    //var nginx = builder.AddContainer("nginx", "nginx", "latest")
    //    .WithBindMount("./nginx.conf", "/etc/nginx/conf", isReadOnly: true)
    //    .WaitFor(edapi)
    //    .WaitFor(admin)
    //    .WithExternalHttpEndpoints();
}

// ===== DESKTOP (dev only) =====
//if (!isPublishMode)
//{
//    builder.AddProject("employeedirectory-desktop",
//        @"..\EmployeeDirectory.Desktop\EmployeeDirectory.Desktop.csproj")
//        .WaitForStart(edapi);
//}

builder.Build().Run();