using System.ComponentModel.DataAnnotations;

namespace Dsm.Managers.Configuration;

public sealed class ManagerOptions
{
    [MinLength(1)]
    public required List<DashboardManagerConfig> DashboardManagers { get; set; }
    public List<string> IgnoredServiceNames { get; set; } = new List<string>();
}
