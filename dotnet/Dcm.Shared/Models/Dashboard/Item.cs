using System.Linq;

namespace Dcm.Shared.Models.Dashboard;
public class Item
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string Icon { get; set; }
    public List<string> Tags { get; set; }

    public override string ToString()
    {
        return $"{nameof(Title)}: {Title}, {nameof(Url)}: {Url}, {nameof(Icon)}: {Icon}, {nameof(Tags)}: {string.Join(",", Tags ?? Enumerable.Empty<string>())}";
    }
}