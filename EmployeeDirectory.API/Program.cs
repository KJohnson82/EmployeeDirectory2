using EmployeeDirectory.Core.Data.Context;
using EmployeeDirectory.Core.DTOs;
using EmployeeDirectory.Core.Services;
using EmployeeDirectory.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<AppDbContext>("employeedirectory-db", configureDbContextOptions: options =>
{
#if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
#endif
});

builder.AddRedisDistributedCache("ed-redis");

// ===== SERVICE REGISTRATION =====
builder.Services.AddScoped<IDirectoryService, DirectoryService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy!"));
app.MapGet("/api/health", () => Results.Ok("Healthy!"));


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(error => error.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal server error");
    }));
}

// ===== DATABASE INITIALIZATION =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking database connection...");

        // TODO: Switch to MigrateAsync once EF Core migrations are set up
        // await context.Database.MigrateAsync();

        // Using EnsureCreatedAsync for initial development (creates schema without migrations)
        // Remove this line when switching to MigrateAsync above
        //await context.Database.EnsureCreatedAsync();

        //logger.LogInformation("Database schema ready.");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed.");
        throw;
    }
}

// ===== MINIMAL API ENDPOINTS =====

// Directory sync — used by Desktop app to pull all data
app.MapGet("/api/directory/sync", async (IDirectoryService service) =>
{
    var employees = await service.GetEmployeesAsync();
    var departments = await service.GetDepartmentsAsync();
    var locations = await service.GetLocationsAsync();

    return Results.Ok(new DirectorySyncDto
    {
        Employees = employees,
        Departments = departments,
        Locations = locations,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("DirectorySync")
.WithTags("Directory");

// Database info — debugging endpoint
app.MapGet("/api/system/dbinfo", async (AppDbContext context) =>
{
    var employeeCount = await context.Employees.CountAsync();
    var departmentCount = await context.Departments.CountAsync();
    var locationCount = await context.Locations.CountAsync();

    return Results.Ok(new
    {
        provider = "PostgreSQL (Aspire)",
        employees = employeeCount,
        departments = departmentCount,
        locations = locationCount,
        timestamp = DateTime.UtcNow
    });
})
.WithName("DatabaseInfo")
.WithTags("System");


//app.UseHttpsRedirection();


app.Run();


