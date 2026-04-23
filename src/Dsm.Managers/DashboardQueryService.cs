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
        var seen = new Dictionary<(string Name, string? Hostname), Service>();
        foreach (var managerConfig in _managerOptions.DashboardManagers)
        {
            var manager = _dashboardManagerFactory.Create(managerConfig);
            foreach (var service in await manager.ListServices())
            {
                seen.TryAdd((service.Name, service.Hostname), service);
            }
        }
        return seen.Values.ToList();
    }
}
