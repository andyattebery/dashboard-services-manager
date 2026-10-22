using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.Options;
using Dsm.Providers.Services;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Options;

namespace Dsm.Providers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class DockerServicesProviderTests : ProviderTestFixtureHostedTestBase
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
    public async Task ListServices_ReturnsServicesFromContainerLabels()
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

    // The router named after the container sorts after the other one, so only a match on the raw
    // container name (adguard-home) picks it; the formatted name (Adguard Home) never matches.
    [Test]
    public async Task MultipleRouters_PrefersRouterNamedAfterContainer()
    {
        var container = new ContainerListResponse
        {
            ID = "abc123",
            Names = new List<string> { "/adguard-home" },
            Labels = new Dictionary<string, string>
            {
                ["traefik.http.routers.adguard-dns.rule"] = "Host(`dns.example.com`)",
                ["traefik.http.routers.adguard-home.rule"] = "Host(`adguard.example.com`)",
            }
        };
        var dockerClient = new Mock<IDockerClient>();
        dockerClient
            .Setup(dc => dc.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContainerListResponse> { container });
        var provider = new DockerServicesProvider(
            new ContainerLabelServiceFactory(NullLogger<ContainerLabelServiceFactory>.Instance),
            dockerClient.Object,
            new ServicesProviderConfig
            {
                ServicesProviderType = ServicesProviderType.Docker,
                DockerLabelPrefix = "dsm",
                AreServiceHostsHttps = true,
                Hostname = "test-host"
            });

        var service = (await provider.ListServices()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(service.Url, Is.EqualTo("https://adguard.example.com"));
            Assert.That(service.Name, Is.EqualTo("Adguard Home"));
        });
    }

    protected override void AddServices(IServiceCollection services)
    {
        base.AddServices(services);

        string dockerContainersJsonPath = TestDataUtilities.GetTestDataPath("docker_containers.json");
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
        services.AddTransient<IOptions<ProviderOptions>>((serviceProvider) => Microsoft.Extensions.Options.Options.Create(providerOptions));
    }
}
