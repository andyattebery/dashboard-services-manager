using Dsm.Managers.Services.IconSources;

namespace Dsm.Managers.Configuration;

public class ServiceDefaultOptions
{
    public List<DashboardIconSourceType> FallbackIconSourceProviders { get; set; } = [];
    public Dictionary<string, CategoryConfig> Categories { get; set; } =
        new Dictionary<string, CategoryConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ServiceConfig> Services { get; set; } =
        new Dictionary<string, ServiceConfig>(StringComparer.OrdinalIgnoreCase);
}