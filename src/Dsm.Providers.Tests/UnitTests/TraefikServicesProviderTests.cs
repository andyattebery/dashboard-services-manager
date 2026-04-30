using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Providers.Options;
using Dsm.Shared.Tests;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class TraefikServicesProviderTests : BaseTest
{
    private IServicesProvider _traefikServicesProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var factory = ServiceProvider.GetRequiredService<ServicesProviderFactory>();
        var config = new ServicesProviderConfig
        {
            ServicesProviderType = ServicesProviderType.Traefik,
            TraefikApiUrl = "http://traefik.test",
            AreServiceHostsHttps = true,
            Hostname = "test-host"
        };
        _traefikServicesProvider = factory.Create(config);
    }

    [Test]
    public async Task ListServicesFiltersAndMapsRouters()
    {
        var services = await _traefikServicesProvider.ListServices();

        Assert.Multiple(() =>
        {
            Assert.That(services, Has.Count.EqualTo(22), "Should skip @internal, disabled, and path-only routers");

            var searxng = services.SingleOrDefault(s => s.Name == "Searxng");
            Assert.That(searxng, Is.Not.Null, "Should strip -docker-compose and @provider suffixes");
            Assert.That(searxng!.Url, Is.EqualTo("https://searxng.example.com"));
            Assert.That(searxng.Hostname, Is.EqualTo("test-host"));

            var traefik = services.SingleOrDefault(s => s.Name == "traefik");
            Assert.That(traefik, Is.Not.Null, "Routers backed by api@internal should be named 'traefik'");
            Assert.That(traefik!.Url, Is.EqualTo("https://traefik.docker-01.example.com"));
            Assert.That(traefik.Hostname, Is.EqualTo("test-host"));

            Assert.That(services.Any(s => s.Url == "https://disabled.example.com"), Is.False, "Disabled routers should be skipped");
            Assert.That(services.Any(s => s.Name == "Path Only"), Is.False, "Routers without a Host(...) rule should be skipped");
        });
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        var jsonPath = TestDataUtilities.GetUnitTestTestDataPath("traefik_routers.json");
        var routers = JsonSerializer.Deserialize<List<TraefikRouter>>(
            File.ReadAllText(jsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var clientMock = new Mock<ITraefikApiClient>();
        clientMock.Setup(c => c.GetRouters()).ReturnsAsync(routers);
        var factoryMock = new Mock<ITraefikApiClientFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(clientMock.Object);
        services.AddSingleton<ITraefikApiClientFactory>(factoryMock.Object);

        var providerOptions = new ProviderOptions
        {
            ApiUrl = "http://dsm.test",
            ServicesProviders = new List<ServicesProviderConfig>()
        };
        services.AddTransient<IOptions<ProviderOptions>>(_ => Microsoft.Extensions.Options.Options.Create(providerOptions));
    }
}
