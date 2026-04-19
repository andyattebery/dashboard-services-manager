using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers.Hosting;
public static class HostBuilderConfiguration
{
    public static void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        services
            .AddOptions<ManagerOptions>()
            .BindConfiguration(nameof(ManagerOptions));
        services
            .AddOptions<ServiceDefaultOptions>()
            .BindConfiguration(nameof(ServiceDefaultOptions));
        services
            .AddTransient<DashboardCommandProcessor, DashboardCommandProcessor>()
            .AddTransient<DashboardQueryService, DashboardQueryService>()
            .AddTransient<DashyDashboardManager, DashyDashboardManager>()
            .AddTransient<DashboardManagerFactory, DashboardManagerFactory>()
            .AddTransient<WithDefaultsServiceFactory, WithDefaultsServiceFactory>();
    }
    
    public static void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddYamlFile("config.yml");
    }
}