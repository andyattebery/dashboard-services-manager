using Microsoft.AspNetCore.Mvc;
using Dsm.Managers;
using Dsm.Shared.Models;

namespace Dsm.Manager.Api.Controllers;

[ApiController]
[Route("dashboard-services")]
public class DashboardController : ControllerBase
{
    private readonly DashboardCommandProcessor _dashboardCommandProcessor;
    private readonly DashboardQueryService _dashboardQueryService;

    public DashboardController(
        DashboardCommandProcessor dashboardCommandProcessor,
        DashboardQueryService dashboardQueryService)
    {
        _dashboardCommandProcessor = dashboardCommandProcessor;
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpPost]
    public async Task<Dictionary<string, List<Service>>> UpdateWithServices(List<Service> services)
    {
        return await _dashboardCommandProcessor.UpdateWithServicesFromProvider(services);
    }

    [HttpGet]
    public async Task<List<Service>> List()
    {
        return await _dashboardQueryService.ListServices();
    }
}
