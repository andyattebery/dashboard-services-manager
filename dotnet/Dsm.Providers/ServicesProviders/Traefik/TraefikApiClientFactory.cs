using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Dsm.Shared.Options;

namespace Dsm.Providers.ServicesProviders.Traefik;

public static class TraefikApiClientFactory
{
    public static ITraefikApiClient Create(IServiceProvider serviceProvider)
    {
        var providerOptions = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
        if (string.IsNullOrWhiteSpace(providerOptions.TraefikApiUrl))
        {
            throw new InvalidOperationException($"{nameof(ProviderOptions)}.{nameof(ProviderOptions.TraefikApiUrl)} must be set.");
        }

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(providerOptions.TraefikApiUrl)
        };
        return RestService.For<ITraefikApiClient>(httpClient);
    }
}
