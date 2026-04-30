using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Services;
using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IValidateOptions<ManagerOptions>, ManagerOptionsValidator>();
        services
            .AddOptions<ServiceDefaultOptions>()
            .BindConfiguration(nameof(ServiceDefaultOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddTransient<DashboardCommandProcessor, DashboardCommandProcessor>()
            .AddTransient<DashboardQueryService, DashboardQueryService>()
            .AddTransient<DashboardManagerFactory, DashboardManagerFactory>()
            .AddTransient<ServiceWithDefaultsFactory, ServiceWithDefaultsFactory>()
            .AddTransient<ServicesCombiner, ServicesCombiner>()
            .AddSingleton<IconResolver, IconResolver>();
        services.AddSingleton<IDashboardIconSource, HomarrLabsDashboardIconSource>();
        services.AddSingleton<IDashboardIconSource, SelfhStDashboardIconSource>();
        services.AddSingleton<IDashboardIconSource, MaterialDesignIconsDashboardIconSource>();
        services.AddHttpClient(HomarrLabsDashboardIconSource.ClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
        services.AddHttpClient(SelfhStDashboardIconSource.ClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
        services.AddHttpClient(MaterialDesignIconsDashboardIconSource.ClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
    }
    
    public static void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        var defaultsPath = Path.Combine(AppContext.BaseDirectory, "service-defaults.yaml");
        configurationBuilder.AddNormalizedYamlFile(defaultsPath, optional: false, reloadOnChange: false);
        configurationBuilder.AddNormalizedYamlFile("manager-config.yml", optional: true);
        configurationBuilder.AddNormalizedYamlFile("manager-config.yaml", optional: true);
        configurationBuilder.AddNormalizedYamlFile("/config/manager-config.yml", optional: true);
        configurationBuilder.AddNormalizedYamlFile("/config/manager-config.yaml", optional: true);
    }
}