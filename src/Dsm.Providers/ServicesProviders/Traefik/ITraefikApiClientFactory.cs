namespace Dsm.Providers.ServicesProviders.Traefik;

public interface ITraefikApiClientFactory
{
    ITraefikApiClient Create(string traefikApiUrl);
}
