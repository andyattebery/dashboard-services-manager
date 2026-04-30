using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dsm.Providers.Services;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Providers.ApiClients;
using Dsm.Providers.Options;
using Dsm.Shared.Configuration;

namespace Dsm.Providers.HostBuilder;
public static class HostBuilderExtensions
{
    public static IServiceCollection AddDsmProviderServices(this IServiceCollection services)
    {
        services.AddOptions<ProviderOptions>()
            .BindConfiguration(nameof(ProviderOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ProviderOptions>, ProviderOptionsValidator>();
        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());
        services.AddSingleton<ServicesProviderFactory, ServicesProviderFactory>();
        services.AddTransient<ContainerLabelServiceFactory, ContainerLabelServiceFactory>();
        services.AddDcmClient();
        services.AddHttpClient(TraefikApiClientFactory.ClientName, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<ITraefikApiClientFactory, TraefikApiClientFactory>();

        return services;
    }

    public static IConfigurationBuilder AddDsmProviderConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddNormalizedYamlFile("provider-config.yml", optional: true);
        configurationBuilder.AddNormalizedYamlFile("provider-config.yaml", optional: true);
        configurationBuilder.AddNormalizedYamlFile("/config/provider-config.yml", optional: true);
        configurationBuilder.AddNormalizedYamlFile("/config/provider-config.yaml", optional: true);

        return configurationBuilder;
    }
}
