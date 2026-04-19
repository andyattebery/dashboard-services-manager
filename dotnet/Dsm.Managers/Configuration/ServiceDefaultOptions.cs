namespace Dsm.Managers.Configuration;

public class DefaultOptions
{
    public bool UseWalkxcodeDashboardIcons { get; set; }
    public Dictionary<string, CategoryConfig> Categories { get; set; } = new Dictionary<string, CategoryConfig>();
    public Dictionary<string, ServiceConfig> Services { get; set; } = new Dictionary<string, ServiceConfig>();
}