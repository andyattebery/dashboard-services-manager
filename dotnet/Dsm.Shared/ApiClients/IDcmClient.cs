using Refit;
using Dsm.Shared.Models;
using Dsm.Shared.Models.Dashboard;

namespace Dsm.Shared.ApiClients;
public interface IDcmClient
{
    [Post("/dashboard-config/update-from-services")]
    Task<List<Section>> UpdateDashboard(IEnumerable<Service> services);
}