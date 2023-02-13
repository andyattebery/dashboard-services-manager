using Dcm.Shared.Models;

namespace Dcm.Provider.ServicesProviders;
public interface IServicesProvider
{
    Task<List<Service>> ListServices();
}