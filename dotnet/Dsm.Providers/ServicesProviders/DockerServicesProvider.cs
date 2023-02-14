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
    private readonly FromProviderServiceFactory _fromProviderServiceFactory;
    private readonly IDockerClient _dockerClient;

    public DockerServicesProvider(
        ILogger<DockerServicesProvider> logger,
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
        var containersListParameters = new ContainersListParameters()
        {
            All = true
        };
        var swarmServices = await _dockerClient.Containers.ListContainersAsync(containersListParameters);
        var services = swarmServices
            .Select(CreateServiceFromContainerListResponse)
            .Where(s => !s.Ignore)
            .ToList();
        return services;
    }

    private Service CreateServiceFromContainerListResponse(ContainerListResponse containerListResponse)
    {
        var serviceName = GetServiceName(containerListResponse);

        return _fromProviderServiceFactory.CreateFromLabels(serviceName, containerListResponse.Labels);
    }

    private static string GetServiceName(ContainerListResponse containerListResponse)
    {
        var name = containerListResponse.Names.First();

        var formattedName = name.TrimStart('/');
        formattedName = ServicesProviderUtilities.GetFormattedServiceName(name);
        return formattedName;
    }
}