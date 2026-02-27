using EmployeeDirectory.Admin.Components;
using EmployeeDirectory.Admin.Services;
using EmployeeDirectory.Core.Data.Context;
using EmployeeDirectory.Core.Services;
using EmployeeDirectory.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelerikBlazor();

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("employeedirectory-db", configureDbContextOptions: options =>
{
    #if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
#endif
    });

builder.Services.AddDbContextFactory<AppDbContext>(lifetime: ServiceLifetime.Scoped);

builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<DirectoryService>();

builder.Services.AddScoped<IAuthService, AuthService>();



// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapDefaultEndpoints();

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
        await context.Database.EnsureCreatedAsync();

        logger.LogInformation("Database schema ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed.");
        throw;
    }
}


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
