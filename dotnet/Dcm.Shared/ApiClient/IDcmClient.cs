using Refit;
using Dcm.Shared.Models;
using Dcm.Shared.Models.Dashboard;

namespace Dcm.Shared.ApiClient;
public interface IDcmClient
{
    [Post("/dashboard-config/update-from-services")]
    Task<List<Section>> UpdateDashboard(IEnumerable<Service> services);
}