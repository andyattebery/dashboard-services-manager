using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.Options;
using Microsoft.Extensions.Options;
using Dsm.Shared.Models;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class DockerServicesProviderTests : BaseTest
{
    private DockerServicesProvider _dockerServicesProvider;

    [SetUp]
    public void SetUp()
    {
        _dockerServicesProvider = ServiceProvider.GetRequiredService<DockerServicesProvider>();
    }

    [Test]
    public async Task Test()
    {
        var services = _dockerServicesProvider.ListServices();
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        string dockerContainersJsonPath = TestDataUtilities.GetUnitTestTestDataPath("docker_containers.json");
        string jsonString = File.ReadAllText(dockerContainersJsonPath);
        var containerListResponses = JsonSerializer.Deserialize<List<ContainerListResponse>>(jsonString)!;

        var dockerClientMock = new Mock<IDockerClient>();

        dockerClientMock
            .Setup(dc => dc.Containers.ListContainersAsync(
                It.IsAny<ContainersListParameters>(),
                default(CancellationToken)).Result)
            .Returns(containerListResponses);

        services.AddTransient<IDockerClient>((serviceProvider) => dockerClientMock.Object);
    }
}