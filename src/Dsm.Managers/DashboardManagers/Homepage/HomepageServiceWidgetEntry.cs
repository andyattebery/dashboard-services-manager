namespace Dsm.Managers.DashboardManagers.Homepage;

public class HomepageServiceWidgetEntry
{
    public string? Server { get; set; }
    public Dictionary<string, object?>? Widget { get; set; }
    public List<Dictionary<string, object?>>? Widgets { get; set; }
}
