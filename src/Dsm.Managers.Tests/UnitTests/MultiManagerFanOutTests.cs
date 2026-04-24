using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Shared.Models;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class MultiManagerFanOutTests : BaseTest
{
    private DashboardCommandProcessor _processor = null!;
    private string _dashyDir = null!;
    private string _homepageDir = null!;
    private string _dashyPath = null!;
    private string _homepagePath = null!;

    [OneTimeSetUp]
    public override void OneTimeSetUp()
    {
    }

    [SetUp]
    public void Setup()
    {
        _dashyDir = Path.Combine(Path.GetTempPath(), $"dsm_dashy_{Guid.NewGuid():N}");
        _homepageDir = Path.Combine(Path.GetTempPath(), $"dsm_homepage_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dashyDir);
        Directory.CreateDirectory(_homepageDir);
        _dashyPath = Path.Combine(_dashyDir, DashyDashboardManager.ConfigFileName);
        _homepagePath = Path.Combine(_homepageDir, HomepageDashboardManager.ServicesFileName);

        File.WriteAllText(_dashyPath, """
            ---
            sections:
            - name: Existing
              items:
              - title: HandWrittenDashyEntry
                url: https://hand.example
                icon: favicon-local
                tags:
                - host=elsewhere
            """);

        File.WriteAllText(_homepagePath, """
            - Existing:
                - HandWrittenHomepageEntry:
                    href: https://hand-hp.example
                    server: elsewhere
            """);

        ServiceProvider = ServiceProviderFactory.Create(ConfigureConfiguration, AddServices);
        _processor = ServiceProvider.GetRequiredService<DashboardCommandProcessor>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var dir in new[] { _dashyDir, _homepageDir })
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task FansOutToBothManagersAndPreservesEachDashboardsHandEntries()
    {
        var incoming = new List<Service>
        {
            new Service("Jellyfin", "https://jellyfin.new", "Media", null, null, "host-1", false),
        };

        var response = await _processor.UpdateWithServicesFromProvider(incoming);

        Assert.Multiple(() =>
        {
            Assert.That(response.Keys, Is.EquivalentTo(new[] { "Dashy", "Homepage" }));
            Assert.That(response["Dashy"].Any(s => s.Name == "Jellyfin"), Is.True);
            Assert.That(response["Dashy"].Any(s => s.Name == "HandWrittenDashyEntry"), Is.True,
                "Dashy's hand-written entry survives in Dashy");
            Assert.That(response["Dashy"].Any(s => s.Name == "HandWrittenHomepageEntry"), Is.False,
                "Homepage's hand-written entry does not leak into Dashy");
            Assert.That(response["Homepage"].Any(s => s.Name == "Jellyfin"), Is.True);
            Assert.That(response["Homepage"].Any(s => s.Name == "HandWrittenHomepageEntry"), Is.True,
                "Homepage's hand-written entry survives in Homepage");
            Assert.That(response["Homepage"].Any(s => s.Name == "HandWrittenDashyEntry"), Is.False,
                "Dashy's hand-written entry does not leak into Homepage");
        });

        var dashyYaml = await File.ReadAllTextAsync(_dashyPath);
        var homepageYaml = await File.ReadAllTextAsync(_homepagePath);
        Assert.Multiple(() =>
        {
            Assert.That(dashyYaml, Does.Contain("https://jellyfin.new"));
            Assert.That(dashyYaml, Does.Contain("HandWrittenDashyEntry"));
            Assert.That(homepageYaml, Does.Contain("https://jellyfin.new"));
            Assert.That(homepageYaml, Does.Contain("HandWrittenHomepageEntry"));
        });
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        services.Configure<ManagerOptions>(opts =>
        {
            opts.DashboardManagers = new List<DashboardManagerConfig>
            {
                new DashboardManagerConfig
                {
                    DashboardManagerType = DashboardManagerType.Dashy,
                    DashboardConfigDirectoryPath = _dashyDir,
                },
                new DashboardManagerConfig
                {
                    DashboardManagerType = DashboardManagerType.Homepage,
                    DashboardConfigDirectoryPath = _homepageDir,
                },
            };
        });

        services.AddTransient<IOptions<ServiceDefaultOptions>>(_ =>
            Options.Create(new ServiceDefaultOptions()));
    }
}
