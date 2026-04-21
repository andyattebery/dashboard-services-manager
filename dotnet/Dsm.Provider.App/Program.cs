using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dsm.Providers.Hosting;

namespace Dsm.Provider.App;
class Program
{
    static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ProviderService>();
                HostBuilderConfiguration.AddServices(hostContext.Configuration, services);
            })
            .ConfigureAppConfiguration((hostContext, configuration) =>
            {
                HostBuilderConfiguration.ConfigureConfiguration(configuration);
                configuration.AddEnvironmentVariables(prefix: "DSM_");
            });
    }
}
