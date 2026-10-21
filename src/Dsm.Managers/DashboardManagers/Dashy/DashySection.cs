namespace Dsm.Managers.DashboardManagers.Dashy;
public class DashySection
{
    public string? Name { get; set; }
    public List<DashyItem> Items { get; set; } = new List<DashyItem>();
}