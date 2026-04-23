using Dsm.Managers.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers.DashboardManagers;

public class DashboardManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DashboardManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDashboardManager Create(DashboardManagerConfig config)
    {
        return config.DashboardManagerType switch
        {
            DashboardManagerType.Dashy =>
                ActivatorUtilities.CreateInstance<DashyDashboardManager>(_serviceProvider, config),
            DashboardManagerType.Homepage =>
                ActivatorUtilities.CreateInstance<HomepageDashboardManager>(_serviceProvider, config),
            _ => throw new ArgumentException(
                $"{config.DashboardManagerType} is not a valid {nameof(DashboardManagerType)}")
        };
    }
}
