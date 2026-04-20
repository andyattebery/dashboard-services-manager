using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Dsm.Providers.ServicesProviders;
using Dsm.Providers.ServicesProviders.Traefik;
using Dsm.Shared.Options;
using Dsm.Shared.Tests;

namespace Dsm.Providers.Tests.UnitTests;

[TestFixture]
public class TraefikServicesProviderTests : BaseTest
{
    private TraefikServicesProvider _traefikServicesProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _traefikServicesProvider = ServiceProvider.GetRequiredService<TraefikServicesProvider>();
    }

    [Test]
    public async Task ListServicesFiltersAndMapsRouters()
    {
        var services = await _traefikServicesProvider.ListServices();

        Assert.Multiple(() =>
        {
            Assert.That(services, Has.Count.EqualTo(2), "Should skip @internal, disabled, and path-only routers");

            var jellyfin = services.SingleOrDefault(s => s.Name == "Jellyfin");
            Assert.That(jellyfin, Is.Not.Null);
            Assert.That(jellyfin!.Url, Is.EqualTo("https://jellyfin.example.com"));
            Assert.That(jellyfin.Hostname, Is.EqualTo("test-host"));

            var searxng = services.SingleOrDefault(s => s.Name == "Searxng");
            Assert.That(searxng, Is.Not.Null, "Should strip -docker-compose and @provider suffixes");
            Assert.That(searxng!.Url, Is.EqualTo("https://search.example.com"));
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
        services.AddTransient<ITraefikApiClient>(_ => clientMock.Object);

        var providerOptions = new ProviderOptions
        {
            ServicesProviderTypes = ["traefik"],
            TraefikApiUrl = "http://traefik.test",
            Hostname = "test-host",
            AreServiceHostsHttps = true
        };
        services.AddTransient<IOptions<ProviderOptions>>(_ => Options.Create(providerOptions));
    }
}
