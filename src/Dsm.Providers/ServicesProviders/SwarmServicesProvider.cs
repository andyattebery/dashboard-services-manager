using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;
using Dsm.Shared.Options;
using Dsm.Providers.Services;

namespace Dsm.Providers.ServicesProviders;
public class SwarmServicesProvider : IServicesProvider
{
    private const string DockerStackNamespaceLabel = "com.docker.stack.namespace";

    private readonly ContainerLabelServiceFactory _containerLabelServiceFactory;
    private readonly IDockerClient _dockerClient;
    private readonly ProviderOptions _providerOptions;
    private readonly ServicesProviderConfig _config;

    public SwarmServicesProvider(
        ContainerLabelServiceFactory containerLabelServiceFactory,
        IDockerClient dockerClient,
        IOptions<ProviderOptions> providerOptions,
        ServicesProviderConfig config
    )
    {
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
        swarmService.Spec.Labels.TryGetValue(DockerStackNamespaceLabel, out var ns);
        return StripStackPrefix(swarmService.Spec.Name, ns);
    }

    internal static string StripStackPrefix(string name, string? stackNamespace)
    {
        if (string.IsNullOrEmpty(stackNamespace))
        {
            return name;
        }
        var prefix = stackNamespace + "_";
        if (name.StartsWith(prefix, StringComparison.Ordinal) && name.Length > prefix.Length)
        {
            return name.Substring(prefix.Length);
        }
        return name;
    }
}
