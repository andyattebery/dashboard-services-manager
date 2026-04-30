using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Dsm.Providers.Options;

namespace Dsm.Providers.ApiClients;

public static class ClientFactory
{
    public static IServiceCollection AddDcmClient(this IServiceCollection services)
    {
        services.AddRefitClient<IDcmClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var apiUrl = sp.GetRequiredService<IOptions<ProviderOptions>>().Value.ApiUrl;
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    throw new InvalidOperationException($"{nameof(ProviderOptions)}.{nameof(ProviderOptions.ApiUrl)} must be set.");
                }
                client.BaseAddress = new Uri(apiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        return services;
    }
}
