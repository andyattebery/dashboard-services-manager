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
            .BindConfiguration(nameof(ManagerOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<ServiceDefaultOptions>()
            .BindConfiguration(nameof(ServiceDefaultOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddTransient<DashboardCommandProcessor, DashboardCommandProcessor>()
            .AddTransient<DashboardQueryService, DashboardQueryService>()
            .AddTransient<DashboardManagerFactory, DashboardManagerFactory>()
            .AddTransient<WithDefaultsServiceFactory, WithDefaultsServiceFactory>()
            .AddTransient<ServicesCombiner, ServicesCombiner>();
        services.AddHttpClient(WithDefaultsServiceFactory.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
    }
    
    public static void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        var defaultsPath = Path.Combine(AppContext.BaseDirectory, "service-defaults.yaml");
        configurationBuilder.AddYamlFile(defaultsPath, optional: false, reloadOnChange: false);
        configurationBuilder.AddYamlFile("manager-config.yml", optional: true);
        configurationBuilder.AddYamlFile("manager-config.yaml", optional: true);
        configurationBuilder.AddYamlFile("/config/manager-config.yml", optional: true);
        configurationBuilder.AddYamlFile("/config/manager-config.yaml", optional: true);
    }
}