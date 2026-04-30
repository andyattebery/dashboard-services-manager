using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dsm.Providers.HostBuilder;
using Dsm.Shared.Configuration;

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
                services.AddDsmProviderServices();
            })
            .ConfigureAppConfiguration((hostContext, configuration) =>
            {
                configuration.AddDsmProviderConfiguration();
                configuration.AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);
            });
    }
}
