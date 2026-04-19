using Dsm.Managers.Configuration;
using Dsm.Managers.Factories;
using Dsm.Shared.Models;
using Dsm.Shared.Options;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class ServiceFactoryTests : BaseTest
{
    private WithDefaultsServiceFactory _withDefaultsServiceFactory;
    
    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        _withDefaultsServiceFactory = ServiceProvider.GetRequiredService<WithDefaultsServiceFactory>();
    }
    
    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        // var defaultOptions = new ServiceDefaultOptions()
        // {
        //     UseWalkxcodeDashboardIcons = true
        // };
        // var options = Options.Create(defaultOptions);
        // services.AddTransient<IOptions<ServiceDefaultOptions>>((serviceProvider) => options);
    }

    [TestCase("Proxmox", "https://cdn.jsdelivr.net/gh/walkxcode/dashboard-icons/png/proxmox.png")]
    [TestCase("AdGuard Home", "https://cdn.jsdelivr.net/gh/walkxcode/dashboard-icons/png/adguard-home.png")]
    [TestCase("Resilio Sync", "https://cdn.jsdelivr.net/gh/walkxcode/dashboard-icons/png/resiliosync.png")]
    [TestCase("Changedetection.io", "https://cdn.jsdelivr.net/gh/walkxcode/dashboard-icons/png/changedetection-io.png")]
    public void WalkxcodeDashboardIcons_Test(string serviceName, string expectedImageUrl)
    {
        var service = new Service()
        {
            Name = serviceName
        };
        var serviceWithDefaults = _withDefaultsServiceFactory.CreateWithDefaults(service);
        
        Assert.That(serviceWithDefaults.ImageUrl, Is.EqualTo(expectedImageUrl));
    }
}