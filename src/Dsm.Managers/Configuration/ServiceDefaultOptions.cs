using Dsm.Managers.Services.IconSources;

namespace Dsm.Managers.Configuration;

public class ServiceDefaultOptions
{
    public List<DashboardIconSourceType> FallbackIconSourceProviders { get; set; } = [];
    public Dictionary<string, CategoryConfig> Categories { get; set; } =
        new Dictionary<string, CategoryConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ServiceConfig> Services { get; set; } =
        new Dictionary<string, ServiceConfig>(StringComparer.OrdinalIgnoreCase);

    // A dashboard group is named by its category's display name, so a group's category is found
    // by key first and then by Name: `nas: { Name: Disks }` is the category of the group `Disks`.
    public CategoryConfig? FindCategory(string keyOrName)
    {
        if (Categories.TryGetValue(keyOrName, out var category)) return category;
        return Categories.Values.FirstOrDefault(c =>
            string.Equals(c.Name, keyOrName, StringComparison.OrdinalIgnoreCase));
    }
}