using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;
using Dsm.Shared.Options;
using Dsm.Providers.Services;

namespace Dsm.Providers.ServicesProviders;
public class SwarmServicesProvider : IServicesProvider
{
    private const string DockerStackNamespaceLabel = "com.docker.stack.namespace";

    private readonly ILogger<SwarmServicesProvider> _logger;
    private readonly ContainerLabelServiceFactory _containerLabelServiceFactory;
    private readonly IDockerClient _dockerClient;
    private readonly ProviderOptions _providerOptions;
    private readonly ServicesProviderConfig _config;

    public SwarmServicesProvider(
        ILogger<SwarmServicesProvider> logger,
        ContainerLabelServiceFactory containerLabelServiceFactory,
        IDockerClient dockerClient,
        IOptions<ProviderOptions> providerOptions,
        ServicesProviderConfig config
    )
    {
        _logger = logger;
        _containerLabelServiceFactory = containerLabelServiceFactory;
        _dockerClient = dockerClient;
        _providerOptions = providerOptions.Value;
        _config = config;
    }

    public async Task<List<Service>> ListServices()
    {
        var swarmServices = await _dockerClient.Swarm.ListServicesAsync();
        var services = swarmServices
            .Select(CreateServiceFromSwarmService)
            .ToList();
        return services;
    }

    private Service CreateServiceFromSwarmService(SwarmService swarmService)
    {
        var serviceName = GetServiceName(swarmService);

        var formattedServiceName = ServicesProviderUtilities.GetFormattedServiceName(serviceName);

        return _containerLabelServiceFactory.CreateFromLabels(_config, _providerOptions.Hostname, formattedServiceName, swarmService.Spec.Labels);
    }

    private static string GetServiceName(SwarmService swarmService)
    {
        var swarmServiceName = swarmService.Spec.Name;

        if (!swarmService.Spec.Labels.TryGetValue(DockerStackNamespaceLabel, out var dockerStackNamespace))
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
