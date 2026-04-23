using Refit;

namespace Dsm.Providers.ServicesProviders.Traefik;

public interface ITraefikApiClientFactory
{
    ITraefikApiClient Create(string traefikApiUrl);
}

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

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(traefikApiUrl);
        return RestService.For<ITraefikApiClient>(httpClient);
    }

    public static string NamedClient => HttpClientName;
}
