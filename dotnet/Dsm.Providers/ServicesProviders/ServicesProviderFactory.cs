using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dsm.Shared.Options;

namespace Dsm.Providers.ServicesProviders;
public static class ServicesProviderFactory
{
    public static IServicesProvider Create(IServiceProvider serviceProvider)
    {
        var providerOptions = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;

        return providerOptions.ProviderType.ToLower() switch
        {
            "docker" => serviceProvider.GetRequiredService<DockerServicesProvider>(),
            "swarm" => serviceProvider.GetRequiredService<SwarmServicesProvider>(),
            "yaml" or "yaml_file" or "yamlfile" => serviceProvider.GetRequiredService<YamlFileServicesProvider>(),
            var providerType => throw new ArgumentException($"{providerType} is not a value provider type.")
        };
    }
}