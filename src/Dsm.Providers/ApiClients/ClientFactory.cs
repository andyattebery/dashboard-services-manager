using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Dsm.Providers.Options;
using Dsm.Shared.Configuration;

namespace Dsm.Providers.ApiClients;

public static class ClientFactory
{
    public static IServiceCollection AddDcmClient(this IServiceCollection services)
    {
        services.AddRefitClient<IDcmClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.ApiUrl))
                {
                    throw new InvalidOperationException($"{nameof(ProviderOptions)}.{nameof(ProviderOptions.ApiUrl)} must be set.");
                }
                client.BaseAddress = new Uri(options.ApiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add(Constants.ApiKeyHeaderName, options.ApiKey);
                }
            });
        return services;
    }
}
