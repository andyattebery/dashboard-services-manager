using Dsm.Managers.Configuration;
using Dsm.Managers.Factories;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers;

public class ManagerCommandProcessor
{
    private readonly ManagerOptions _managerOptions;
    private readonly DashboardManagerFactory _dashboardManagerFactory;
    private readonly WithDefaultsServiceFactory _withDefaultsServiceFactory;
    
    public ManagerCommandProcessor(
        IOptions<ManagerOptions> managerOptions,
        DashboardManagerFactory dashboardManagerFactory, 
        WithDefaultsServiceFactory withDefaultsServiceFactory)
    {
        _managerOptions = managerOptions.Value;
        _dashboardManagerFactory = dashboardManagerFactory;
        _withDefaultsServiceFactory = withDefaultsServiceFactory;
    }

    public List<Service> UpdateWithServicesFromProvider(IEnumerable<Service> providerServices)
    {
        var newServicesWithDefaults = providerServices
            .Select(_withDefaultsServiceFactory.CreateWithDefaults)
            .ToList();
        var dashboardManager = _dashboardManagerFactory.Create(_managerOptions.DashboardManagerType);
        var existingServices = dashboardManager.ListServices();
        var combinedServices = CombineExistingServicesWithNewServices(existingServices, newServicesWithDefaults);
        dashboardManager.UpdateWithNewServices(combinedServices);
        return combinedServices;
    }

    private static List<Service> CombineExistingServicesWithNewServices(
        List<Service> existingServices, List<Service> newServices)
    {
        var combinedServices = new List<Service>(newServices);
        var nameAndHostnameToNewService = newServices.ToDictionary(s => (s.Name, s.Hostname));
        var nameToNewServiceMapping = newServices.ToDictionary(s => s.Name);
        var areAllNewServicesForSameHost = newServices.DistinctBy(s => s.Hostname).Count() == 1;
        foreach (var existingService in existingServices)
        {
            var existingServiceNameAndHostname = (existingService.Name, existingService.Hostname);

            if (// New service with the same name
                nameToNewServiceMapping.ContainsKey(existingService.Name) || 
                // New services are all for the same host and don't have existing service with
                // the same name for the host
                (areAllNewServicesForSameHost &&
                 !nameAndHostnameToNewService.ContainsKey(existingServiceNameAndHostname)))
            {
                continue;
            }

            combinedServices.Add(existingService);
        }

        return combinedServices;
    }
}