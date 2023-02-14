using System.Linq;
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
        var services = await _dockerServicesProvider.ListServices();

        var gitaService = services.SingleOrDefault(s => s.Name == "Gitea");

        Assert.Multiple(() => {
            var giteaService = services.SingleOrDefault(s => s.Name == "Gitea");
            Assert.That(giteaService, Is.Not.Null, "Could not find gitea service");
            Assert.That(giteaService.Url, Is.EqualTo("https://gitea.omegaho.me"));

            var minioService = services.SingleOrDefault(s => s.Name == "Minio");
            Assert.That(minioService, Is.Not.Null, "Could not find minio service");
            Assert.That(minioService.Url, Is.EqualTo("https://minio.omegaho.me"));

            var scrutinyService = services.SingleOrDefault(s => s.Name == "Scrutiny");
            Assert.That(scrutinyService, Is.Not.Null, "Could not find scrutiny service");
            Assert.That(scrutinyService.Url, Is.EqualTo("https://scrutiny.omegaho.me"));
        });
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

        var providerOptions = new ProviderOptions()
        {
            ProviderType = "docker",
            DockerLabelPrefix = "dsm",
            AreServiceHostsHttps = true
        };
        var options = Options.Create(providerOptions);
        services.AddTransient<IOptions<ProviderOptions>>((serviceProvider) => options);
    }

    private async Task SaveDockerContainerListResponsesJson()
    {
        var dockerClient = ServiceProvider.GetRequiredService<IDockerClient>();
        var containersListParameters = new ContainersListParameters()
        {
            All = true
        };
        var containerListResponses = await dockerClient.Containers.ListContainersAsync(containersListParameters);

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(containerListResponses, options);
        File.WriteAllText(TestDataUtilities.GetUnitTestTestDataPath("docker_containers.json"), jsonString);
    }
}