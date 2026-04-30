using Refit;

namespace Dsm.Providers.ServicesProviders.Traefik;

public sealed class TraefikApiClientFactory : ITraefikApiClientFactory
{
    private const string HttpClientName = "traefik";

    private readonly IHttpClientFactory _httpClientFactory;

    public TraefikApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ITraefikApiClient Create(string traefikApiUrl)
    {
        if (string.IsNullOrWhiteSpace(traefikApiUrl))
        {
            throw new InvalidOperationException($"{nameof(traefikApiUrl)} must be set.");
        }

        // CreateClient returns a fresh HttpClient instance each call (the pooled piece
        // is the HttpMessageHandler), so setting BaseAddress here mutates only this
        // instance — safe for per-provider Traefik URLs.
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(traefikApiUrl);
        return RestService.For<ITraefikApiClient>(httpClient);
    }

    public static string NamedClient => HttpClientName;
}
