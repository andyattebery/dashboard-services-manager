using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers;
public class DashboardManagerFactory
{
    private IServiceProvider _serviceProvider;

    public DashboardManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDashboardManager Create(DashboardManagerType dashboardManagerType)
    {
        return dashboardManagerType switch
        {
            DashboardManagerType.Dashy => _serviceProvider.GetRequiredService<DashyDashboardManager>(),
            _ => throw new ArgumentException($"{dashboardManagerType} is not a valid {nameof(DashboardManagerType)}")
        };
    }
}