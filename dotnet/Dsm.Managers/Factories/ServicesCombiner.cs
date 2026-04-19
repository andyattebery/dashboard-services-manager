using Dsm.Shared.Models;

namespace Dsm.Managers.Factories;

public class ServicesCombiner
{
    public List<Service> CombineExistingServicesWithNewServices(
        List<Service> existingServices,
        List<Service> newServices)
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