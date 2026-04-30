using Microsoft.Extensions.DependencyInjection;
using Dsm.Providers.Options;

namespace Dsm.Providers.ServicesProviders;
public class ServicesProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ServicesProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServicesProvider Create(ServicesProviderConfig config)
    {
        return config.ServicesProviderType switch
        {
            ServicesProviderType.Docker => ActivatorUtilities.CreateInstance<DockerServicesProvider>(_serviceProvider, config),
            ServicesProviderType.Swarm => ActivatorUtilities.CreateInstance<SwarmServicesProvider>(_serviceProvider, config),
            ServicesProviderType.YamlFile => ActivatorUtilities.CreateInstance<YamlFileServicesProvider>(_serviceProvider, config),
            ServicesProviderType.Traefik => ActivatorUtilities.CreateInstance<TraefikServicesProvider>(_serviceProvider, config),
            _ => throw new ArgumentException($"{config.ServicesProviderType} is not a valid {nameof(ServicesProviderType)}.")
        };
    }
}
