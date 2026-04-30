using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class DashboardQueryServiceDedupTests : BaseTest
{
    private DashboardQueryService _queryService = null!;
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
        _dashyDir = Path.Combine(Path.GetTempPath(), $"dsm_dedup_dashy_{Guid.NewGuid():N}");
        _homepageDir = Path.Combine(Path.GetTempPath(), $"dsm_dedup_hp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dashyDir);
        Directory.CreateDirectory(_homepageDir);
        _dashyPath = Path.Combine(_dashyDir, DashyDashboardManager.ConfigFileName);
        _homepagePath = Path.Combine(_homepageDir, HomepageDashboardManager.ServicesFileName);

        // Dashy: Jellyfin@media-01 + DashyOnly@media-01
        File.WriteAllText(_dashyPath, """
            ---
            sections:
            - name: Media
              items:
              - title: Jellyfin
                url: https://jellyfin.dashy
                icon: hl-jellyfin
                tags:
                - host=media-01
              - title: DashyOnly
                url: https://dashy-only.example
                icon: favicon-local
                tags:
                - host=media-01
            """);

        // Homepage: Jellyfin@media-01 (should be deduped — Dashy wins) + HomepageOnly@media-01
        File.WriteAllText(_homepagePath, """
            - Media:
                - Jellyfin:
                    href: https://jellyfin.homepage
                    server: media-01
                - HomepageOnly:
                    href: https://hp-only.example
                    server: media-01
            """);

        ServiceProvider = ServiceProviderFactory.Create(ConfigureConfiguration, AddServices);
        _queryService = ServiceProvider.GetRequiredService<DashboardQueryService>();
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
    public async Task ListServices_DedupesAcrossManagersByNameAndHostname_FirstWins()
    {
        var services = await _queryService.ListServices();

        var jellyfinMatches = services.Where(s => s.Name == "Jellyfin").ToList();
        Assert.That(jellyfinMatches, Has.Count.EqualTo(1), "(Name, Hostname) collision should dedupe");
        Assert.That(jellyfinMatches[0].Url, Is.EqualTo("https://jellyfin.dashy"),
            "First manager (Dashy) wins on conflict");

        Assert.That(services.Any(s => s.Name == "DashyOnly"), Is.True);
        Assert.That(services.Any(s => s.Name == "HomepageOnly"), Is.True,
            "Entries unique to later managers still survive");
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
