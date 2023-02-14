using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Dsm.Shared.Models;
using Dsm.Providers.Services;

namespace Dsm.Providers.ServicesProviders;
public class SwarmServicesProvider : IServicesProvider
{
    private readonly Regex SwarmServiceNameRegex = new Regex(@"^.+_(.*)");
    private const string DockerStackNamespaceLabel = "com.docker.stack.namespace";

    private readonly ILogger<SwarmServicesProvider> _logger;
    private readonly FromProviderServiceFactory _fromProviderServiceFactory;
    private readonly IDockerClient _dockerClient;

    public SwarmServicesProvider(
        ILogger<SwarmServicesProvider> logger,
        FromProviderServiceFactory fromProviderServiceFactory,
        IDockerClient dockerClient
    )
    {
        _logger = logger;
        _fromProviderServiceFactory = fromProviderServiceFactory;
        _dockerClient = dockerClient;
    }

    public async Task<List<Service>> ListServices()
    {
        var swarmServices = await _dockerClient.Swarm.ListServicesAsync();
        var services = swarmServices
            .Select(CreateServiceFromSwarmService)
            .Where(s => !s.Ignore)
            .ToList();
        return services;
    }

    private Service CreateServiceFromSwarmService(SwarmService swarmService)
    {
        var serviceName = GetServiceName(swarmService, false);

        var formattedServiceName = ServicesProviderUtilities.GetFormattedServiceName(serviceName);

        return _fromProviderServiceFactory.CreateFromLabels(formattedServiceName, swarmService.Spec.Labels);
    }

    private static string GetServiceName(SwarmService swarmService, bool includeStackName)
    {
        var swarmServiceName = swarmService.Spec.Name;

        if (includeStackName ||
            !swarmService.Spec.Labels.TryGetValue(DockerStackNamespaceLabel, out var dockerStackNamespace))
        {
            return swarmServiceName;
        }

        var serviceNameRegex = new Regex(@$"^{dockerStackNamespace}_(.+)$");
        var serviceNameRegexMatch = serviceNameRegex.Match(swarmServiceName);
        var serviceName = serviceNameRegexMatch.Success &&
            !string.IsNullOrEmpty(serviceNameRegexMatch.Groups[1].Value) ?
            serviceNameRegexMatch.Groups[1].Value :
            swarmServiceName;

        return serviceName;
    }
}