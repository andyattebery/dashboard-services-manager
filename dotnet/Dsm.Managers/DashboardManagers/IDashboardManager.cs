using Dsm.Shared.Models;

namespace Dsm.Managers;
public interface IDashboardManager
{
    List<Service> ListServices();
    void UpdateWithNewServices(List<Service> services);
}