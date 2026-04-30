using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Models;

namespace Dsm.Managers.DashboardManagers;
public interface IDashboardManager
{
    Task<List<Service>> ListServices();
    Task WriteServices(List<Service> services);

    /// <summary>
    /// Sources this dashboard renders natively, mapped to the prefix the dashboard expects in
    /// its YAML icon field. An empty prefix means the dashboard's default lookup (no prefix).
    /// When a service's icon matches one of these sources, DSM passes the (possibly retranslated)
    /// prefix-form name through instead of fetching the CDN URL.
    /// </summary>
    IReadOnlyDictionary<DashboardIconSourceType, string> NativeIconSourcePrefixes { get; }
}
