using EmployeeDirectory.Core.Services;
using EmployeeDirectory.Desktop.Data;
using EmployeeDirectory.Desktop.Interfaces;
using EmployeeDirectory.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeDirectory.Desktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // ===== CONFIGURATION =====
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#else
    .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true)
#endif
            .Build();

        builder.Configuration.AddConfiguration(config);

        // ===== TELERIK BLAZOR =====
        builder.Services.AddTelerikBlazor();

        // ===== LOCAL SQLITE CACHE =====
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "employeedirectory-cache.db");
        builder.Services.AddDbContext<LocalCacheContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // ===== HTTP CLIENT =====
        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7100";

        builder.Services.AddHttpClient("EmployeeDirectoryApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ===== SERVICES =====
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<ICacheService, CacheService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddScoped<IDirectoryService, CachedDirectoryService>();
        builder.Services.AddScoped<ISearchService, CachedSearchService>();

        // ===== INITIALIZE DATABASE =====
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LocalCacheContext>();
            context.Database.EnsureCreated();
        }

        return app;
    }
}