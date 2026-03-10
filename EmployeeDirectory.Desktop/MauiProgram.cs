using EmployeeDirectory.Core.Services;
using EmployeeDirectory.Desktop.Components.Layout;
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

        // ===== BLAZOR =====
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // ===== CONFIGURATION =====
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var configBuilder = new ConfigurationBuilder();

        using var appSettingsStream = assembly.GetManifestResourceStream(
            "EmployeeDirectory.Desktop.appsettings.json");
        if (appSettingsStream != null)
            configBuilder.AddJsonStream(appSettingsStream);

#if DEBUG
        using var devStream = assembly.GetManifestResourceStream(
            "EmployeeDirectory.Desktop.appsettings.Development.json");
        if (devStream != null)
            configBuilder.AddJsonStream(devStream);
#else
        using var prodStream = assembly.GetManifestResourceStream(
            "EmployeeDirectory.Desktop.appsettings.Production.json");
        if (prodStream != null)
            configBuilder.AddJsonStream(prodStream);
#endif

        builder.Configuration.AddConfiguration(configBuilder.Build());

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
        builder.Services.AddSingleton<LayoutState>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<ICacheService, CacheService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddScoped<IDirectoryService, CachedDirectoryService>();
        builder.Services.AddScoped<ISearchService, CachedSearchService>();

        // ===== BUILD & INITIALIZE DATABASE =====
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LocalCacheContext>();
            context.Database.EnsureCreated();
        }

        return app;
    }
}