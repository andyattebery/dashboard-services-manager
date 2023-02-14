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
            var providerType => throw new ArgumentException($"{providerType} is not a valid provider type.")
        };
    }

    public IServicesProvider Create(string servicesProviderTypeString)
    {
        var servicesProviderType = GetServiceProviderType(servicesProviderTypeString);
        return Create(servicesProviderType);
    }

    private ServicesProviderType GetServiceProviderType(string serviceProviderTypeString)
    {
        return serviceProviderTypeString.ToLower() switch
        {
            "docker" => ServicesProviderType.Docker,
            "swarm" => ServicesProviderType.Swarm,
            "yaml" or "yaml_file" or "yamlfile" => ServicesProviderType.YamlFile,
            var providerType => throw new ArgumentException($"{providerType} is not a value provider type.")
        };
    }
}