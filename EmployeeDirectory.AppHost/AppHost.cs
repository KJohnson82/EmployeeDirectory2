
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("ed-postgres")
    .WithDataVolume("employeedirectory-pgdata")
    .WithLifetime(ContainerLifetime.Session)
    .WithPgAdmin();

var employeeDirectoryDb = postgres.AddDatabase("employeedirectory-db");


var redis = builder.AddRedis("ed-redis")
    .WithDataVolume("employeedirectory-redis-data")
    .WithLifetime(ContainerLifetime.Session);

var edapi = builder.AddProject<Projects.EmployeeDirectory_API>("employeedirectory-api")
    .WithReference(employeeDirectoryDb)
    .WaitFor(employeeDirectoryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

var admin = builder.AddProject<Projects.EmployeeDirectory_Admin>("employeedirectory-admin")
    .WithReference(employeeDirectoryDb)
    .WaitFor(employeeDirectoryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();


builder.AddProject<Projects.EmployeeDirectory_Desktop>("employeedirectory-desktop").WaitForStart(edapi);


builder.Build().Run();
