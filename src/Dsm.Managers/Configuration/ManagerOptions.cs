using System.ComponentModel.DataAnnotations;

namespace Dsm.Managers.Configuration;

public sealed class ManagerOptions
{
    [MinLength(1)]
    public required List<DashboardManagerConfig> DashboardManagers { get; set; }
    public List<string> IgnoredServiceNames { get; set; } = new List<string>();

    // When set, requests to /dashboard-services must present a matching X-Api-Key header.
    // When null/empty, the endpoint is unauthenticated — the trusted-LAN default.
    public string? ApiKey { get; set; }
}
