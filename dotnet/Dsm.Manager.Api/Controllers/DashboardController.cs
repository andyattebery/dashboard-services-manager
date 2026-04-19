using Microsoft.AspNetCore.Mvc;
using Dsm.Managers;
using Dsm.Shared.Models;

namespace Dsm.Manager.Api.Controllers;

[ApiController]
[Route("dashboard-services")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;
    private readonly DashboardCommandProcessor _dashboardCommandProcessor;
    private readonly DashboardQueryService _dashboardQueryService;

    public DashboardController(ILogger<DashboardController> logger, DashboardCommandProcessor dashboardCommandProcessor, DashboardQueryService dashboardQueryService)
    {
        _logger = logger;
        _dashboardCommandProcessor = dashboardCommandProcessor;
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpPost]
    public async Task<List<Service>> UpdateWithServices(List<Service> services)
    {
        return await _dashboardCommandProcessor.UpdateWithServicesFromProvider(services);
    }
    
    [HttpGet]
    public async Task<List<Service>> List()
    {
        return await _dashboardQueryService.ListServices();
    }
}
