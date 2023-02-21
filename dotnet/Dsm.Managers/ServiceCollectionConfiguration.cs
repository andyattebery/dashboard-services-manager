using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsm.Shared.Options;

namespace Dsm.Managers;
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