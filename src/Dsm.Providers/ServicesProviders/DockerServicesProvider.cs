using Docker.DotNet;
using Docker.DotNet.Models;
using Dsm.Shared.Models;
using Dsm.Providers.Options;
using Dsm.Providers.Services;

namespace Dsm.Providers.ServicesProviders;
public class DockerServicesProvider : IServicesProvider
{
    private readonly ContainerLabelServiceFactory _containerLabelServiceFactory;
    private readonly IDockerClient _dockerClient;
    private readonly ServicesProviderConfig _config;

    public DockerServicesProvider(
        ContainerLabelServiceFactory containerLabelServiceFactory,
        IDockerClient dockerClient,
        ServicesProviderConfig config
    )
    {
        _containerLabelServiceFactory = containerLabelServiceFactory;
        _dockerClient = dockerClient;
        _config = config;
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

        return _containerLabelServiceFactory.CreateFromLabels(_config, _config.Hostname, serviceName, containerListResponse.Labels);
    }

    private static string GetServiceName(ContainerListResponse containerListResponse)
    {
        var name = containerListResponse.Names?.FirstOrDefault() ?? containerListResponse.ID;

        var formattedName = name.TrimStart('/');
        formattedName = ServicesProviderUtilities.GetFormattedServiceName(formattedName);
        return formattedName;
    }
}
