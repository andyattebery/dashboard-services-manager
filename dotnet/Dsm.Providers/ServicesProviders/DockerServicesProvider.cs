using System.Linq;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Dsm.Shared.Models;
using Dsm.Providers.Services;

namespace Dsm.Providers.ServicesProviders;
public class DockerServicesProvider : IServicesProvider
{
    private readonly ILogger<DockerServicesProvider> _logger;
    private readonly ContainerLabelServiceFactory _containerLabelServiceFactory;
    private readonly IDockerClient _dockerClient;

    public DockerServicesProvider(
        ILogger<DockerServicesProvider> logger,
        ContainerLabelServiceFactory containerLabelServiceFactory,
        IDockerClient dockerClient
    )
    {
        _logger = logger;
        _containerLabelServiceFactory = containerLabelServiceFactory;
        _dockerClient = dockerClient;
    }

    public async Task<List<Service>> ListServices()
    {
        var containersListParameters = new ContainersListParameters()
        {
            All = true
        };
        var swarmServices = await _dockerClient.Containers.ListContainersAsync(containersListParameters);
        var services = swarmServices
            .Select(CreateServiceFromContainerListResponse)
            .ToList();
        return services;
    }

    private Service CreateServiceFromContainerListResponse(ContainerListResponse containerListResponse)
    {
        var serviceName = GetServiceName(containerListResponse);

        return _containerLabelServiceFactory.CreateFromLabels(serviceName, containerListResponse.Labels);
    }

    private static string GetServiceName(ContainerListResponse containerListResponse)
    {
        var name = containerListResponse.Names.First();

        var formattedName = name.TrimStart('/');
        formattedName = ServicesProviderUtilities.GetFormattedServiceName(formattedName);
        return formattedName;
    }
}