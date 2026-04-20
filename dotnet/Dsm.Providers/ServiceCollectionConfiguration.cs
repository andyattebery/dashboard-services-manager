using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.Services;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.ServicesProviders.Traefik;
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
        services.AddTransient<TraefikServicesProvider, TraefikServicesProvider>();
        services.AddTransient<ServicesProviderFactory, ServicesProviderFactory>();
        services.AddTransient<ContainerLabelServiceFactory, ContainerLabelServiceFactory>();
        services.AddTransient<IDcmClient>(ClientFactory.CreateDcmClient);
        services.AddTransient<ITraefikApiClient>(TraefikApiClientFactory.Create);
    }
}