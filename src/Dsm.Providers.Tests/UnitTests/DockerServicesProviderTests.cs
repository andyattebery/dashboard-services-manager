using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Dsm.Providers.ServicesProviders;
using Dsm.Shared.Options;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Options;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class DockerServicesProviderTests : BaseTest
{
    private IServicesProvider _dockerServicesProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var factory = ServiceProvider.GetRequiredService<ServicesProviderFactory>();
        var config = new ServicesProviderConfig
        {
            ServicesProviderType = ServicesProviderType.Docker,
            DockerLabelPrefix = "dsm",
            AreServiceHostsHttps = true,
            Hostname = "test-host"
        };
        _dockerServicesProvider = factory.Create(config);
    }

    [Test]
    public async Task Test()
    {
        var services = await _dockerServicesProvider.ListServices();

        Assert.Multiple(() =>
        {
            var giteaService = services.SingleOrDefault(s => s.Name == "Gitea");
            Assert.That(giteaService, Is.Not.Null, "Could not find gitea service");
            Assert.That(giteaService!.Url, Is.EqualTo("https://gitea.omegaho.me"));

            var minioService = services.SingleOrDefault(s => s.Name == "Minio");
            Assert.That(minioService, Is.Not.Null, "Could not find minio service");
            Assert.That(minioService!.Url, Is.EqualTo("https://minio.omegaho.me"));

            var scrutinyService = services.SingleOrDefault(s => s.Name == "Scrutiny");
            Assert.That(scrutinyService, Is.Not.Null, "Could not find scrutiny service");
            Assert.That(scrutinyService!.Url, Is.EqualTo("https://scrutiny.omegaho.me"));
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

        var providerOptions = new ProviderOptions
        {
            ApiUrl = "http://dsm.test",
            ServicesProviders = new List<ServicesProviderConfig>()
        };
        services.AddTransient<IOptions<ProviderOptions>>((serviceProvider) => Options.Create(providerOptions));
    }
}
