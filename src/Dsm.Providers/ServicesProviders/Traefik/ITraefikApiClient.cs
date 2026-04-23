using Refit;

namespace Dsm.Providers.ServicesProviders.Traefik;

public interface ITraefikApiClient
{
    [Get("/api/http/routers")]
    Task<List<TraefikRouter>> GetRouters();
}
