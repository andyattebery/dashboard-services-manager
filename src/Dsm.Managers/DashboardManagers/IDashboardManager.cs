using Dsm.Shared.Models;

namespace Dsm.Managers.DashboardManagers;
public interface IDashboardManager
{
    Task<List<Service>> ListServices();
    Task WriteServices(List<Service> services);
}