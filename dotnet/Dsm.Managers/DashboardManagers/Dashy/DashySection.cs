namespace Dsm.Managers.Dashy;
public class DashySection
{
    public DashySection(string name, string? icon, List<DashyItem> items)
    {
        Name = name;
        Icon = icon;
        Items = items;
    }

    [Obsolete("For serialization")]
    public DashySection()
    {
    }

    public string? Name { get; set; }
    public string? Icon { get; set; }
    public List<DashyItem> Items { get; set; } = new List<DashyItem>();
}