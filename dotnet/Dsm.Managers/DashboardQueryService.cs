using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers;

public class DashboardQueryService
{
    private readonly ManagerOptions _managerOptions;
    private readonly DashboardManagerFactory _dashboardManagerFactory;

    public DashboardQueryService(
        IOptions<ManagerOptions> managerOptions,
        DashboardManagerFactory dashboardManagerFactory)
    {
        _managerOptions = managerOptions.Value;
        _dashboardManagerFactory = dashboardManagerFactory;
    }

    public async Task<List<Service>> ListServices()
    {
        var dashboardManager = _dashboardManagerFactory.Create(_managerOptions.DashboardManagerType);
        return await dashboardManager.ListServices();
    }
}