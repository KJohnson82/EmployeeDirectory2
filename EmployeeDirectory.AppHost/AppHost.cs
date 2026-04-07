using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Yarp;
using Aspire.Hosting.Yarp.Transforms;

var builder = DistributedApplication.CreateBuilder(args);

var isPublishMode = builder.ExecutionContext.IsPublishMode;

// ===== DOCKER COMPOSE ENVIRONMENT =====
builder.AddDockerComposeEnvironment("employeedirectory-env")
    .WithProperties(env => { env.DashboardEnabled = true; })
    .WithDashboard(dashboard =>
    {
        dashboard.WithHostPort(7070)
            .PublishAsDockerComposeService((resource, service) => { service.Name = "ed-env"; })
            .WithForwardedHeaders(enabled: true);
    });

// ===== PRIVATE REGISTRY =====
#pragma warning disable ASPIRECOMPUTE003, ASPIREPIPELINES003
var registry = builder.AddContainerRegistry("ed-registry", "10.169.176.246:5000");
#pragma warning restore ASPIRECOMPUTE003, ASPIREPIPELINES003

// ===== DATABASE =====
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("ed-postgres", password: postgresPassword)
    .WithDataVolume("employeedirectory-pgdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-postgres";
        service.Restart = "unless-stopped";
    })
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050));

var employeeDirectoryDb = postgres.AddDatabase("employeedirectory-db");

// ===== CACHE =====
var redis = builder.AddRedis("ed-redis")
    .WithDataVolume("employeedirectory-redis-data")
    .WithLifetime(isPublishMode ? ContainerLifetime.Persistent : ContainerLifetime.Session)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-redis";
        service.Restart = "unless-stopped";
    });

// ===== API =====
#pragma warning disable ASPIRECOMPUTE003, ASPIREPIPELINES003
var edapi = builder.AddProject<Projects.EmployeeDirectory_API>("ed-api")
    .WithReference(employeeDirectoryDb).WaitFor(employeeDirectoryDb)
    .WithReference(redis).WaitFor(redis)
    .WithContainerRegistry(registry)
    .WithRemoteImageTag("latest")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-api";
        service.Restart = "unless-stopped";
    });
#pragma warning restore ASPIRECOMPUTE003, ASPIREPIPELINES003

// ===== ADMIN =====
#pragma warning disable ASPIRECOMPUTE003, ASPIREPIPELINES003
var admin = builder.AddProject<Projects.EmployeeDirectory_Admin>("ed-admin")
    .WithReference(employeeDirectoryDb).WaitFor(employeeDirectoryDb)
    .WithReference(redis).WaitFor(redis)
    .WithContainerRegistry(registry)
    .WithRemoteImageTag("latest")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-admin";
        service.Restart = "unless-stopped";
    });
#pragma warning restore ASPIRECOMPUTE003, ASPIREPIPELINES003

// ===== YARP GATEWAY =====
builder.AddYarp("ed-gateway")
    .WithHostPort(80)
    .WithConfiguration(yarp =>
    {
        // Specific API route first — YARP matches top-down
        yarp.AddRoute("/api/{**catch-all}", edapi)
            .WithMatchMethods("GET", "POST", "PUT", "DELETE", "PATCH")
            .WithTransformPathRemovePrefix("/api");
        // Catch-all to admin for everything else
        yarp.AddRoute("/{**catch-all}", admin);
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "ed-gateway";
        service.Restart = "unless-stopped";
        // Fix: Aspire generates expose: "5000" — we need the actual host port mapping.
        // The YARP container listens internally on 5000 (verify against your
        // generated compose if this ever changes — check the expose value).
        service.Ports = new List<string> { "80:5000" };
        service.Expose = new List<string>(); // clear the generated expose
        service.DependsOn = new Dictionary<string, ServiceDependency>
        {
            ["ed-api"] = new ServiceDependency { Condition = "service_started" },
            ["ed-admin"] = new ServiceDependency { Condition = "service_started" }
        };
    });

builder.Build().Run();




//using Aspire.Hosting.Yarp;
//using Aspire.Hosting.Yarp.Transforms;
//using Aspire.Hosting.Docker.Resources.ComposeNodes;


//var builder = DistributedApplication.CreateBuilder(args);

//// ===== CONFIGURATION FLAGS =====
//var isPublishMode = builder.ExecutionContext.IsPublishMode;

//// ===== DOCKER COMPOSE ENVIRONMENT (for publish/deploy) =====

//builder.AddDockerComposeEnvironment("employeedirectory-env")
//    .WithProperties(env =>
//    {
//        env.DashboardEnabled = true;
//    })
//    .WithDashboard(dashboard =>
//    {
//        dashboard.WithHostPort(7070)
//        .PublishAsDockerComposeService((resource, service) =>
//        {
//            service.Name = "ed-env";
//        })
//            .WithForwardedHeaders(enabled: true);
//    });


//// ===== DATABASE =====
//var postgresPassword = builder.AddParameter("postgres-password", secret: true);
//var postgres = builder.AddPostgres("ed-postgres", password: postgresPassword)
//    .WithDataVolume("employeedirectory-pgdata")
//    .WithLifetime(ContainerLifetime.Persistent)
//    .PublishAsDockerComposeService((resource, service) =>
//    {
//        service.Name = "ed-postgres";

//    })
//    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050));

//var employeeDirectoryDb = postgres.AddDatabase("employeedirectory-db");

//// ===== CACHE =====
//var redis = builder.AddRedis("ed-redis")
//    .WithDataVolume("employeedirectory-redis-data")
//    .WithLifetime(isPublishMode ? ContainerLifetime.Persistent : ContainerLifetime.Session)
//    .PublishAsDockerComposeService((resource, service) =>
//    {
//        service.Name = "ed-redis";
//    });

//// ===== API =====
//var edapi = builder.AddProject<Projects.EmployeeDirectory_API>("ed-api")
//    .WithReference(employeeDirectoryDb)
//    .WaitFor(employeeDirectoryDb)
//    .WithReference(redis)
//    .WaitFor(redis)
//    .WithExternalHttpEndpoints()
//    .PublishAsDockerComposeService((resource, service) =>
//    {
//        service.Name = "ed-api";
//    });

//// ===== ADMIN =====
//var admin = builder.AddProject<Projects.EmployeeDirectory_Admin>("ed-admin")
//    .WithReference(employeeDirectoryDb)
//    .WaitFor(employeeDirectoryDb)
//    .WithReference(redis)
//    .WaitFor(redis)
//    .WithExternalHttpEndpoints()
//    .PublishAsDockerComposeService((resource, service) =>
//    {
//        service.Name = "ed-admin";
//    });

//// ==== YARP Setup ===
//var gateway = builder.AddYarp("ed-gateway")
//    .WithHostPort(80)
//    //.WithHttpsEndpoint()
//    .WithConfiguration(yarp =>
//    {
//        yarp.AddRoute(edapi);
//        yarp.AddRoute(admin);

//        yarp.AddRoute("/api/{**catch-all}", edapi)
//            .WithMatchMethods("GET", "POST", "PUT", "DELETE", "PATCH")
//            .WithTransformPathRemovePrefix("/api");

//        //yarp.AddRoute("/health", edapi);

//        //yarp.AddRoute("/admin/{**catch-all}", admin)
//        //.WithTransformPathRemovePrefix("/admin");

//        yarp.AddRoute("/{**catch-all}", admin)
//        .WithTransformPathRemovePrefix("/admin");

//    });

//// ===== DESKTOP (dev only) =====
////if (!isPublishMode)
////{
////    builder.AddProject("employeedirectory-desktop",
////        @"..\EmployeeDirectory.Desktop\EmployeeDirectory.Desktop.csproj")
////        .WaitForStart(edapi);
////}

//builder.Build().Run();