using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;
using Dsm.Shared.Options;
using Dsm.Providers.Services;

namespace Dsm.Providers.ServicesProviders;
public class DockerServicesProvider : IServicesProvider
{
    private readonly ContainerLabelServiceFactory _containerLabelServiceFactory;
    private readonly IDockerClient _dockerClient;
    private readonly ProviderOptions _providerOptions;
    private readonly ServicesProviderConfig _config;

    public DockerServicesProvider(
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

        return _containerLabelServiceFactory.CreateFromLabels(_config, _providerOptions.Hostname, serviceName, containerListResponse.Labels);
    }

    private static string GetServiceName(ContainerListResponse containerListResponse)
    {
        var name = containerListResponse.Names?.FirstOrDefault() ?? containerListResponse.ID;

        var formattedName = name.TrimStart('/');
        formattedName = ServicesProviderUtilities.GetFormattedServiceName(formattedName);
        return formattedName;
    }
}
