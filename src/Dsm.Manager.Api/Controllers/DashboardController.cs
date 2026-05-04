using Microsoft.AspNetCore.Mvc;
using Dsm.Managers;
using Dsm.Shared.Models;
using Serilog;

namespace Dsm.Manager.Api.Controllers;

[ApiController]
[Route("dashboard-services")]
public class DashboardController : ControllerBase
{
    private readonly IDiagnosticContext _diagnosticContext;
    private readonly DashboardCommandProcessor _dashboardCommandProcessor;
    private readonly DashboardQueryService _dashboardQueryService;

    public DashboardController(
        IDiagnosticContext diagnosticContext,
        DashboardCommandProcessor dashboardCommandProcessor,
        DashboardQueryService dashboardQueryService)
    {
        _diagnosticContext = diagnosticContext;
        _dashboardCommandProcessor = dashboardCommandProcessor;
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpPost]
    public async Task<Dictionary<string, List<Service>>> UpdateWithServices(List<Service> services)
    {
        _diagnosticContext.Set("ServiceCount", services.Count);
        return await _dashboardCommandProcessor.UpdateWithServicesFromProvider(services);
    }

    [HttpGet]
    public async Task<List<Service>> List()
    {
        return await _dashboardQueryService.ListServices();
    }
}
