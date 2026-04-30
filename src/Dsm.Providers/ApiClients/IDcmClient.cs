using Refit;
using Dsm.Shared.Models;

namespace Dsm.Providers.ApiClients;
public interface IDcmClient
{
    [Post("/dashboard-services")]
    Task<Dictionary<string, List<Service>>> UpdateDashboard(IEnumerable<Service> services);
}
