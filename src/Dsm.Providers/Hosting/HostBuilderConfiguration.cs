using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dsm.Providers.Services;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Shared.ApiClients;
using Dsm.Shared.Options;

namespace Dsm.Providers.Hosting;
public static class HostBuilderConfiguration
{
    public static void AddServices(IConfiguration configuration, IServiceCollection services)
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
        services.AddHttpClient(TraefikApiClientFactory.NamedClient, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<ITraefikApiClientFactory, TraefikApiClientFactory>();
    }

    public static void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddYamlFile("provider-config.yml", optional: true);
        configurationBuilder.AddYamlFile("provider-config.yaml", optional: true);
        configurationBuilder.AddYamlFile("/config/provider-config.yml", optional: true);
        configurationBuilder.AddYamlFile("/config/provider-config.yaml", optional: true);
    }
}
