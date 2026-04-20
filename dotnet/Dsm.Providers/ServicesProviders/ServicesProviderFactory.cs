using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Providers.ServicesProviders;
public class ServicesProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ServicesProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServicesProvider Create(ServicesProviderType servicesProviderType)
    {
        return servicesProviderType switch
        {
            ServicesProviderType.Docker => _serviceProvider.GetRequiredService<DockerServicesProvider>(),
            ServicesProviderType.Swarm => _serviceProvider.GetRequiredService<SwarmServicesProvider>(),
            ServicesProviderType.YamlFile => _serviceProvider.GetRequiredService<YamlFileServicesProvider>(),
            ServicesProviderType.Traefik => _serviceProvider.GetRequiredService<TraefikServicesProvider>(),
            _ => throw new ArgumentException($"{servicesProviderType} is not a valid {nameof(ServicesProviderType)}.")
        };
    }

    public IServicesProvider Create(string servicesProviderTypeString)
    {
        var servicesProviderType = GetServiceProviderType(servicesProviderTypeString);
        return Create(servicesProviderType);
    }

    private static ServicesProviderType GetServiceProviderType(string servicesProviderTypeString)
    {
        return servicesProviderTypeString.ToLower() switch
        {
            "docker" => ServicesProviderType.Docker,
            "swarm" => ServicesProviderType.Swarm,
            "yaml" or "yaml_file" or "yamlfile" => ServicesProviderType.YamlFile,
            "traefik" => ServicesProviderType.Traefik,
            _ => throw new ArgumentException($"{servicesProviderTypeString} is not a valid {nameof(ServicesProviderType)}.")
        };
    }
}