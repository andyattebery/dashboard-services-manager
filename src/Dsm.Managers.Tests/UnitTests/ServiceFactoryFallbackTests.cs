using Dsm.Managers.Configuration;
using Dsm.Managers.Services;
using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class ServiceFactoryFallbackTests : BaseTest
{
    private IEnumerable<IDashboardIconSource> _iconSources = null!;

    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        _iconSources = ServiceProvider.GetRequiredService<IEnumerable<IDashboardIconSource>>();
    }

    private ServiceWithDefaultsFactory CreateFactory(params DashboardIconSourceType[] fallback)
    {
        var options = Options.Create(new ServiceDefaultOptions
        {
            FallbackIconSourceProviders = fallback.ToList()
        });
        return new ServiceWithDefaultsFactory(options, _iconSources);
    }

    [Test]
    public async Task FallbackEmpty_DoesNotProbeAnySource()
    {
        var factory = CreateFactory();
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await factory.CreateWithDefaultsAsync(service);

        Assert.Multiple(() =>
        {
            Assert.That(result.Icon, Is.Null);
            Assert.That(result.ImageUrl, Is.Null);
        });
    }

    [Test]
    [Category("Network")]
    public async Task FallbackHomarrLabsOnly_UsesHomarrLabsUrl()
    {
        var factory = CreateFactory(DashboardIconSourceType.HomarrLabs);
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await factory.CreateWithDefaultsAsync(service);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
    }

    [Test]
    [Category("Network")]
    public async Task FallbackHomarrLabsThenSelfhSt_HomarrLabsWinsForSharedIcon()
    {
        var factory = CreateFactory(DashboardIconSourceType.HomarrLabs, DashboardIconSourceType.SelfhSt);
        var service = new Service("Jellyfin", "https://example.com", null, null, null, null, false);

        var result = await factory.CreateWithDefaultsAsync(service);

        Assert.That(result.ImageUrl, Is.EqualTo("https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/jellyfin.png"));
    }
}
