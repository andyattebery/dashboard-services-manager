using Dsm.Managers.Configuration;
using Dsm.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers.Di;
public static class ServiceCollectionConfiguration
{
    public static void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        services.Configure<ManagerOptions>(
            configuration.GetSection(nameof(ManagerOptions)));
        services.AddTransient<DashyDashboardManager, DashyDashboardManager>();
        services.AddTransient<DashboardManagerFactory, DashboardManagerFactory>();
    }
}