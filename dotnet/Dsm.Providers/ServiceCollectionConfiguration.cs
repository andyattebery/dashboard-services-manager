using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.Services;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.ApiClients;
using Dsm.Shared.Options;

namespace Dsm.Providers;
public static class ServiceCollectionConfiguration
{
    public static void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        services.Configure<ProviderOptions>(
            configuration.GetSection(nameof(ProviderOptions)));
        services.AddTransient<IDockerClient>((context) => new DockerClientConfiguration().CreateClient());
        services.AddTransient<DockerServicesProvider, DockerServicesProvider>();
        services.AddTransient<SwarmServicesProvider, SwarmServicesProvider>();
        services.AddTransient<YamlFileServicesProvider, YamlFileServicesProvider>();
        services.AddTransient<ServicesProviderFactory, ServicesProviderFactory>();
        services.AddTransient<FromProviderServiceFactory, FromProviderServiceFactory>();
        services.AddTransient<IDcmClient>(ClientFactory.CreateDcmClient);
    }
}