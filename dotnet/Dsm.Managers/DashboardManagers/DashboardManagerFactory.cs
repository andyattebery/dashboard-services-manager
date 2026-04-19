using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers;
public class DashboardManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

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

    public IDashboardManager Create(string dashboardManagerTypeString)
    {
        var dashboardManagerType = GetDashboardManagerType(dashboardManagerTypeString);
        return Create(dashboardManagerType);
    }

    private DashboardManagerType GetDashboardManagerType(string dashboardManagerTypeString)
    {
        return dashboardManagerTypeString.ToLower() switch
        {
            "dashy" => DashboardManagerType.Dashy,
            _ => throw new ArgumentException(
                $"{dashboardManagerTypeString} is not a valid {nameof(DashboardManagerType)}")
        };
    }
}