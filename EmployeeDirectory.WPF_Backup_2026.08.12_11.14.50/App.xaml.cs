using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Windows;

namespace EmployeeDirectory.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public IServiceProvider Services { get; set;  } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddServiceDiscovery();
                    services.AddHttpClient<edapi>(client =>
                    {
                        client.BaseAddress = new Uri("https+http://api");
                    }).AddServiceDiscovery();
                })
                .Build();

            Services = host.Services;
            host.Start();
        }

    }

}
