using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dcm.Shared.Options;

namespace Dcm.Provider.ServicesProviders;
public static class ServicesProviderFactory
{
    public static IServicesProvider Create(IServiceProvider serviceProvider)
    {
        var providerOptions = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;

        return providerOptions.ProviderType switch
        {
            "swarm" => serviceProvider.GetRequiredService<SwarmServicesProvider>(),
            var providerType => throw new ArgumentException($"{providerType} is not a value provider type.")
        };
    }
}