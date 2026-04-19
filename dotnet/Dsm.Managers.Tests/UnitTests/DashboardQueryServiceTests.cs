using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers.Tests.UnitTests;

public class DashboardQueryServiceTests : BaseTest
{
    private DashboardQueryService _dashboardQueryService;
    
    [SetUp]
    public void Setup()
    {
        _dashboardQueryService = ServiceProvider.GetRequiredService<DashboardQueryService>();
    }

    public async Task Test()
    {
        var services = await _dashboardQueryService.ListServices();
    }
}