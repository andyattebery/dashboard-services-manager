using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Factories;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers;

public class DashboardCommandProcessor
{
    private readonly ManagerOptions _managerOptions;
    private readonly DashboardManagerFactory _dashboardManagerFactory;
    private readonly WithDefaultsServiceFactory _withDefaultsServiceFactory;
    private readonly ServicesCombiner _servicesCombiner;

    public DashboardCommandProcessor(
        IOptions<ManagerOptions> managerOptions,
        DashboardManagerFactory dashboardManagerFactory,
        WithDefaultsServiceFactory withDefaultsServiceFactory,
        ServicesCombiner servicesCombiner)
    {
        _managerOptions = managerOptions.Value;
        _dashboardManagerFactory = dashboardManagerFactory;
        _withDefaultsServiceFactory = withDefaultsServiceFactory;
        _servicesCombiner = servicesCombiner;
    }

    public async Task<List<Service>> UpdateWithServicesFromProvider(IEnumerable<Service> providerServices)
    {
        var newServicesWithDefaults = providerServices
            .Select(_withDefaultsServiceFactory.CreateWithDefaults)
            .ToList();
        var dashboardManager = _dashboardManagerFactory.Create(_managerOptions.DashboardManagerType);
        var existingServices = await dashboardManager.ListServices();
        var combinedServices =
            _servicesCombiner.CombineExistingServicesWithNewServices(existingServices, newServicesWithDefaults);
        await dashboardManager.UpdateWithNewServices(combinedServices);
        return combinedServices;
    }
}