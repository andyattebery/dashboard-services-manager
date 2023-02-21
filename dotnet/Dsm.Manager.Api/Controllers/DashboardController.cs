using Microsoft.AspNetCore.Mvc;
using Dsm.Managers;
using Dsm.Shared.Models;

namespace Dsm.Manager.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;
    private readonly DashboardManagerFactory _dashboardManagerFactory;

    public DashboardController(ILogger<DashboardController> logger, DashboardManagerFactory dashboardManagerFactory)
    {
        _logger = logger;
        _dashboardManagerFactory = dashboardManagerFactory;
    }

    [HttpPost()]
    public List<Service> UpdateWithServices(List<Service> services)
    {
        var dashboardManager = _dashboardManagerFactory.Create(DashboardManagerType.Dashy);
        return dashboardManager.UpdateWithServices(services);
    }
}
