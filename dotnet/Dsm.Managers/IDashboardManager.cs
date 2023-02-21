using Dsm.Shared.Models;

namespace Dsm.Managers;
public interface IDashboardManager
{
    List<Service> UpdateWithServices(List<Service> services);
}