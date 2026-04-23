using Dsm.Shared.Models;

namespace Dsm.Providers.ServicesProviders;
public interface IServicesProvider
{
    Task<List<Service>> ListServices();
}