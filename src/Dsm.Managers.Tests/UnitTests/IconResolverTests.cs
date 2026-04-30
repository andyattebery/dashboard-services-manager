using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Services;
using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class IconResolverTests : BaseTest
{
    private IEnumerable<IDashboardIconSource> _iconSources = null!;

    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        _iconSources = ServiceProvider.GetRequiredService<IEnumerable<IDashboardIconSource>>();
    }

    private IconResolver CreateResolver(params DashboardIconSourceType[] fallback)
    {
        var options = Options.Create(new ServiceDefaultOptions
        {
            FallbackIconSourceProviders = fallback.ToList()
        });
        return new IconResolver(options, _iconSources);
    }

    private static IDashboardManager StubManager(params (DashboardIconSourceType Type, string Prefix)[] native)
    {
        return new StubDashboardManager(native.ToDictionary(t => t.Type, t => t.Prefix));
    }

    private sealed class StubDashboardManager : IDashboardManager
    {
        public StubDashboardManager(IReadOnlyDictionary<DashboardIconSourceType, string> native)
            => NativeIconSourcePrefixes = native;
        public IReadOnlyDictionary<DashboardIconSourceType, string> NativeIconSourcePrefixes { get; }
        public Task<List<Service>> ListServices() => throw new NotSupportedException();
        public Task WriteServices(List<Service> services) => throw new NotSupportedException();
    }

    [Test]
    public async Task NativePrefix_PassesThroughWithoutTranslation()
    {
        var resolver = CreateResolver();
        var manager = StubManager((DashboardIconSourceType.MaterialDesignIcons, "mdi-"));
        var service = new Service("Foo", "https://example.com", null, "mdi-account", null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("mdi-account"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task NativePrefix_TranslatesPrefix_WhenManagerUsesDifferentPrefix()
    {
        var resolver = CreateResolver();
        var manager = StubManager((DashboardIconSourceType.HomarrLabs, ""));
        var service = new Service("Foo", "https://example.com", null, "hl-jellyfin", null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("jellyfin"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task NonNativePrefix_FallsBackToCdnProbe()
    {
        var resolver = CreateResolver();
        var manager = StubManager(); // no native sources
        var service = new Service("Foo", "https://example.com", null, "hl-jellyfin", null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
        });
    }

    [Test]
    [Category("Network")]
    public async Task NonNativePrefix_CdnMiss_KeepsOriginalIcon()
    {
        var resolver = CreateResolver();
        var manager = StubManager(); // no native sources
        var service = new Service("Foo", "https://example.com", null, "hl-definitely-not-a-real-icon-xyz", null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("hl-definitely-not-a-real-icon-xyz"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task FallbackChain_NativeSource_ProbesThenStampsMatchedName()
    {
        // Native: HomarrLabs at empty prefix (Homepage default). Probe must run so we know the
        // matched name variant ("jellyfin", lowercase) and avoid stamping a non-existent icon.
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager((DashboardIconSourceType.HomarrLabs, ""));
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("jellyfin"));   // matched-name from CDN
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task FallbackChain_NativeSource_MissReturnsNull()
    {
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager((DashboardIconSourceType.HomarrLabs, ""));
        var service = new Service("Definitely Not A Real Service Xyz", "https://example.com", null, null, null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task FallbackChain_HomarrLabsOnly_UsesHomarrLabsUrl_WhenNonNative()
    {
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager(); // no native sources
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
    }

    [Test]
    [Category("Network")]
    public async Task FallbackChain_HomarrLabsThenSelfhSt_HomarrLabsWinsForSharedIcon_WhenNonNative()
    {
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs, DashboardIconSourceType.SelfhSt);
        var manager = StubManager(); // no native sources
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
    }

    [Test]
    public async Task FallbackChain_Empty_ReturnsBothNull()
    {
        var resolver = CreateResolver();
        var manager = StubManager();
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task FallbackChain_UsesServiceDefaultsName_AsLookupName()
    {
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager(); // no native sources, so we hit the CDN
        var service = new Service("My Custom Name", "https://example.com", null, null, null, null, false, serviceDefaultsName: "Plex");

        var result = await resolver.Resolve(service, manager);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/plex.png"));
    }

    [TestCase("Portainer", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/portainer.png")]
    [TestCase("AdGuard Home", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/adguard-home.png")]
    [TestCase("AdGuard Home Sync", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/adguard-home-sync.png")]
    [TestCase("Resilio Sync", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/resiliosync.png")]
    [TestCase("Flexget", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/flexget.png")]
    [TestCase("Plex", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/plex.png")]
    [TestCase("Prometheus", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/prometheus.png")]
    [TestCase("Scrutiny", "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/scrutiny.png")]
    [Category("Network")]
    public async Task FallbackChain_HomarrLabsIcons_Smoke(string serviceName, string expectedImageUrl)
    {
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager(); // non-native, so we get the URL form
        var service = new Service(serviceName, "https://example.com", null, null, null, null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.That(result.ImageUrl, Is.EqualTo(expectedImageUrl));
    }

    // --- ResolveIcon (category-style) tests --------------------------------------------------

    [Test]
    public async Task ResolveIcon_NativePrefix_PassesThrough()
    {
        var resolver = CreateResolver();
        var manager = StubManager((DashboardIconSourceType.MaterialDesignIcons, "mdi-"));

        var result = await resolver.ResolveIcon("mdi-network", manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("mdi-network"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task ResolveIcon_NativePrefix_TranslatesToManagerPrefix()
    {
        var resolver = CreateResolver();
        var manager = StubManager((DashboardIconSourceType.HomarrLabs, ""));

        var result = await resolver.ResolveIcon("hl-jellyfin", manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("jellyfin"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task ResolveIcon_NonNativePrefix_FallsBackToCdn()
    {
        var resolver = CreateResolver();
        var manager = StubManager(); // no native sources

        var result = await resolver.ResolveIcon("hl-jellyfin", manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
        });
    }

    [Test]
    [Category("Network")]
    public async Task ResolveIcon_NonNativePrefix_CdnMiss_PassesThroughVerbatim()
    {
        var resolver = CreateResolver();
        var manager = StubManager(); // no native sources

        var result = await resolver.ResolveIcon("hl-definitely-not-a-real-icon-xyz", manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("hl-definitely-not-a-real-icon-xyz"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task ResolveIcon_NoPrefix_PassesThroughVerbatim()
    {
        var resolver = CreateResolver();
        var manager = StubManager();

        var result = await resolver.ResolveIcon("fas fa-network-wired", manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("fas fa-network-wired"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task ResolveIcon_NullInput_ReturnsNullPair_NoFallbackChain()
    {
        // Even with a populated fallback chain, a category with no icon set returns null —
        // unlike services, categories don't auto-pick from the fallback.
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager();

        var result = await resolver.ResolveIcon(null, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task ResolveIcon_EmptyInput_ReturnsNullPair()
    {
        var resolver = CreateResolver(DashboardIconSourceType.HomarrLabs);
        var manager = StubManager();

        var result = await resolver.ResolveIcon("", manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    // --- end ResolveIcon tests --------------------------------------------------------------

    [Test]
    public async Task ImageUrlAlreadySet_PassesThrough()
    {
        var resolver = CreateResolver();
        var manager = StubManager((DashboardIconSourceType.MaterialDesignIcons, "mdi-"));
        var service = new Service("Foo", "https://example.com", null, null, "https://my.cdn/foo.png", null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://my.cdn/foo.png"));
        });
    }

    [Test]
    public async Task NativePrefix_WinsOverImageUrl()
    {
        // When a service has both Icon (with native prefix) and ImageUrl, the prefix path wins —
        // matches the pre-refactor factory's precedence.
        var resolver = CreateResolver();
        var manager = StubManager((DashboardIconSourceType.MaterialDesignIcons, "mdi-"));
        var service = new Service("Foo", "https://example.com", null, "mdi-account", "https://my.cdn/foo.png", null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("mdi-account"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task NonNativePrefix_CdnMiss_FallsBackToImageUrl()
    {
        // Prefix matches but CDN misses — the resolver should fall through to ImageUrl rather
        // than returning the original prefix string. Matches the pre-refactor factory's behaviour.
        var resolver = CreateResolver();
        var manager = StubManager(); // non-native: prefix triggers probe
        var service = new Service(
            "Foo", "https://example.com", null,
            "hl-definitely-not-a-real-icon-xyz",
            "https://my.cdn/foo.png",
            null, false);

        var result = await resolver.Resolve(service, manager);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://my.cdn/foo.png"));
        });
    }
}
