using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Shared.Models;
using Dsm.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class DashboardCommandProcessorTests : BaseTest
{
    private DashboardCommandProcessor _dashboardCommandProcessor;

    private const string DashboardConfigFilePath = $"{nameof(DashboardCommandProcessorTests)}_dashy_conf.yml";
    
    
    [SetUp]
    public void Setup()
    {
        _dashboardCommandProcessor = ServiceProvider.GetRequiredService<DashboardCommandProcessor>();
        File.CreateText(DashboardConfigFilePath);
    }

    [Test]
    public async Task UpdateServicesTest()
    {
        var services = new List<Service>()
        {
            new Service("flood", "https://flood.example.com", "", "", "", "", false)
        };
        var updatedServices = await _dashboardCommandProcessor.UpdateWithServicesFromProvider(services);
        
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);
        
        var managerOptions = new ManagerOptions()
        {
            DashboardManagerType = DashboardManagerType.Dashy,
            DashboardConfigFilePath = DashboardConfigFilePath
        };
        var options = Options.Create(managerOptions);
        services.AddTransient<IOptions<ManagerOptions>>((serviceProvider) => options);
    }
}