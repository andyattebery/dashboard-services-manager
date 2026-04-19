using Refit;
using Dsm.Shared.Models;

namespace Dsm.Shared.ApiClients;
public interface IDcmClient
{
    [Post("/dashboard-services")]
    Task<List<Service>> UpdateDashboard(IEnumerable<Service> services);
}