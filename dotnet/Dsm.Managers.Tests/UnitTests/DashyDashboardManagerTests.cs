using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Tests;
using Dsm.Shared.Models;
using Dsm.Shared.Options;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class DashyDashboardManagerTests : BaseTest
{
    private DashyDashboardManager _dashyDashboardManager;
    
    [SetUp]
    public void Setup()
    {
        _dashyDashboardManager = ServiceProvider.GetRequiredService<DashyDashboardManager>();
    }

    [Test]
    public void ListServicesFromDashboard()
    {
        var services = _dashyDashboardManager.ListServices();
    }
    
    [Test]
    public async Task UpdateWithServices()
    {
        var services = new List<Service>()
        {
            new Service("flood", "https://flood.example.com", "", "", "", "", false)
        };
        await _dashyDashboardManager.UpdateWithNewServices(services);
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);
        
        var providerOptions = new ManagerOptions()
        {
            DashboardConfigFilePath = TestDataUtilities.GetTestDataPath("dashy_conf.yml"),
            DashboardManagerType = DashboardManagerType.Dashy
        };
        var options = Options.Create(providerOptions);
        services.AddTransient<IOptions<ManagerOptions>>((serviceProvider) => options);
    }
}