using System.Linq;

namespace Dcm.Shared.Models.Dashboard;
public class Section
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public List<Item> Items { get; set; }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Icon)}: {Icon}, {nameof(Items)}: {string.Join(",", Items ?? Enumerable.Empty<Item>())}";
    }
}