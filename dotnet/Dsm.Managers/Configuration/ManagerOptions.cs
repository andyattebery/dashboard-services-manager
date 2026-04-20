namespace Dsm.Managers.Configuration;

public sealed class ManagerOptions
{
    public required List<DashboardManagerConfig> DashboardManagers { get; set; }
    public List<string> IgnoredServiceNames { get; set; } = new List<string>();
}
