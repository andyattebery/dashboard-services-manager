using Dsm.Managers.Configuration;
using Dsm.Managers.Services;
using Dsm.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class ServiceFactoryTests : BaseTest
{
    private ServiceWithDefaultsFactory _serviceWithDefaultsFactory;

    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        _serviceWithDefaultsFactory = ServiceProvider.GetRequiredService<ServiceWithDefaultsFactory>();
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
    public async Task HomarrLabsDashboardIcons_Test(string serviceName, string expectedImageUrl)
    {
        var service = new Service(serviceName, "https://example.com", null, null, null, null, false);
        var serviceWithDefaults = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(serviceWithDefaults.ImageUrl, Is.EqualTo(expectedImageUrl));
    }

    [Test]
    public async Task DefaultImagePath_AppliedFromServiceDefaults()
    {
        var service = new Service("Proxmox", "https://example.com", null, null, null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/proxmox.png"));
    }

    [Test]
    public async Task ServiceDefaultsName_OverridesDefaultsLookup()
    {
        var service = new Service("PiKVM HID", "https://example.com", null, null, null, null, false, serviceDefaultsName: "Proxmox");

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.Category, Is.EqualTo("infrastructure"));
    }

    [Test]
    [Category("Network")]
    public async Task ServiceDefaultsName_OverridesHomarrLabsIconProbe()
    {
        var service = new Service("My Custom Name", "https://example.com", null, null, null, null, false, serviceDefaultsName: "Plex");

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/plex.png"));
    }

    [Test]
    [Category("Network")]
    public async Task HlPrefixedIcon_ResolvesToImageUrlAndClearsIcon()
    {
        var service = new Service("Something Else", "https://example.com", null, "hl-jellyfin", null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
        });
    }

    [Test]
    [Category("Network")]
    public async Task HlPrefixedIcon_IsCaseInsensitive()
    {
        var service = new Service("Something Else", "https://example.com", null, "HL-jellyfin", null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
        });
    }

    [Test]
    [Category("Network")]
    public async Task HlPrefixedIcon_NoMatchAtCdn_KeepsOriginalIcon()
    {
        var service = new Service("Something Else", "https://example.com", null, "hl-definitely-not-a-real-icon-xyz", null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("hl-definitely-not-a-real-icon-xyz"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task ShPrefixedIcon_ResolvesToImageUrlAndClearsIcon()
    {
        var service = new Service("Something Else", "https://example.com", null, "sh-jellyfin", null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/selfhst/icons/png/jellyfin.png"));
        });
    }

    [Test]
    [Category("Network")]
    public async Task ShPrefixedIcon_IsCaseInsensitive()
    {
        var service = new Service("Something Else", "https://example.com", null, "SH-jellyfin", null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/selfhst/icons/png/jellyfin.png"));
        });
    }

    [Test]
    [Category("Network")]
    public async Task ShPrefixedIcon_NoMatchAtCdn_KeepsOriginalIcon()
    {
        var service = new Service("Something Else", "https://example.com", null, "sh-definitely-not-a-real-icon-xyz", null, null, false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.EqualTo("sh-definitely-not-a-real-icon-xyz"));
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    public async Task NameWithHostnameFormatString_IsApplied()
    {
        var service = new Service("Traefik", "https://example.com", null, null, null, "host1", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.Name, Is.EqualTo("Traefik (host1)"));
    }

    [Test]
    public async Task RelativeImagePath_IsResolvedAgainstServiceUrl()
    {
        var service = new Service("Grafana", "https://grafana.example.com", null, null, "/public/img/grafana_icon.svg", "host1", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.ImageUrl, Is.EqualTo("https://grafana.example.com/public/img/grafana_icon.svg"));
    }

    [Test]
    public async Task AbsoluteImagePath_PassesThroughUnchanged()
    {
        var service = new Service("Healthchecks", "https://healthchecks.example.com", null, null, "https://healthchecks.io/static/img/favicon.png", "host1", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.ImageUrl, Is.EqualTo("https://healthchecks.io/static/img/favicon.png"));
    }

    [Test]
    public async Task DefaultsLookup_IsCaseInsensitive()
    {
        var service = new Service("TRAEFIK", "https://example.com", null, null, null, "host1", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.Name, Is.EqualTo("Traefik (host1)"));
        Assert.That(result.Category, Is.EqualTo("network"));
    }

    [Test]
    public async Task NameWithHostnameFormat_AllowsBraceChar()
    {
        var service = new Service("Traefik", "https://example.com", null, null, null, "a{b}c", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.Name, Is.EqualTo("Traefik (a{b}c)"));
    }

    [Test]
    public async Task ServiceDefaultsName_IsStampedFromDefaultsLookupKey()
    {
        var service = new Service("Adguardhome", "https://example.com", null, null, null, "network-01", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("AdGuard Home (network-01)"));
            Assert.That(result.ServiceDefaultsName, Is.EqualTo("Adguardhome"));
        });
    }

    [Test]
    public async Task ServiceDefaultsName_IsNullWhenNoDefaultsMatch()
    {
        var service = new Service("NoSuchService", "https://example.com", null, null, null, "host1", false);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.ServiceDefaultsName, Is.Null);
    }

    [Test]
    public async Task ServiceDefaultsName_ExplicitValueIsPreserved()
    {
        var service = new Service("My Custom Name", "https://example.com", null, null, null, "host1", false, serviceDefaultsName: "Proxmox");

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.ServiceDefaultsName, Is.EqualTo("Proxmox"));
    }

    [Test]
    public async Task AutogeneratedFlag_IsPreserved()
    {
        var service = new Service("SomeService", "https://example.com", "media", null, null, "h", false, autogenerated: true);

        var result = await _serviceWithDefaultsFactory.CreateWithDefaultsAsync(service);

        Assert.That(result.Autogenerated, Is.True);
    }
}
