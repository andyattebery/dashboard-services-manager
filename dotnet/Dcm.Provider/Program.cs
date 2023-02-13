using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dcm.Provider.Services;
using Dcm.Provider.ServicesProviders;
using Dcm.Shared.ApiClient;
using Dcm.Shared.Options;

namespace Dcm.Provider;
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
                services.Configure<ProviderOptions>(
                    hostContext.Configuration.GetSection(nameof(ProviderOptions)));
                services.AddTransient<IDockerClient>((context) => new DockerClientConfiguration().CreateClient());
                services.AddTransient<SwarmServicesProvider, SwarmServicesProvider>();
                services.AddTransient<IServicesProvider>(ServicesProviderFactory.Create);
                services.AddTransient<FromProviderServiceFactory, FromProviderServiceFactory>();
                services.AddTransient<IDcmClient>(ClientFactory.CreateDcmClient);
            })
            .ConfigureAppConfiguration((hostContext, configuration) =>
            {
                configuration.AddEnvironmentVariables(prefix: "DCM_");
            });
    }
}
