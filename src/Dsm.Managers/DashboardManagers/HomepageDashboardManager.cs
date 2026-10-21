using System.Globalization;
using System.Text.RegularExpressions;
using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers.Homepage;
using Dsm.Managers.Services;
using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dsm.Managers.DashboardManagers;

public class HomepageDashboardManager : IDashboardManager
{
    public const string ServicesFileName = "services.yaml";
    public const string SettingsFileName = "settings.yaml";
    private const string LayoutKey = "layout";
    private const string IconKey = "icon";

    private const string UncategorizedGroup = "Uncategorized";
    private static readonly Regex LetterAfterDigit = new(@"(?<=\d)[a-z]", RegexOptions.Compiled);

    private readonly DashboardManagerConfig _config;
    private readonly ServiceDefaultOptions _serviceDefaultOptions;
    private readonly IconResolver _iconResolver;
    private readonly ILogger<HomepageDashboardManager> _logger;

    public HomepageDashboardManager(
        DashboardManagerConfig config,
        IOptions<ServiceDefaultOptions> defaultOptions,
        IconResolver iconResolver,
        ILogger<HomepageDashboardManager> logger)
    {
        _config = config;
        _serviceDefaultOptions = defaultOptions.Value;
        _iconResolver = iconResolver;
        _logger = logger;
    }

    public IReadOnlyDictionary<DashboardIconSourceType, string> NativeIconSourcePrefixes { get; }
        = new Dictionary<DashboardIconSourceType, string>
        {
            [DashboardIconSourceType.HomarrLabs] = "",
            [DashboardIconSourceType.SelfhSt] = "sh-",
            [DashboardIconSourceType.MaterialDesignIcons] = "mdi-",
        };

    public DashboardManagerType Type => DashboardManagerType.Homepage;
    public string ConfigFilePath => ServicesFilePath;

    private string ServicesFilePath => Path.Combine(_config.DashboardConfigDirectoryPath, ServicesFileName);
    private string SettingsFilePath => Path.Combine(_config.DashboardConfigDirectoryPath, SettingsFileName);

    public async Task<List<Service>> ListServices()
    {
        var entries = await LoadEntries();
        return entries.Select(e => e.Service).ToList();
    }

    public async Task<bool> WriteServices(List<Service> services)
    {
        var widgets = await LoadWidgetEntries();
        var handWritten = YamlNodes.IndexHandWritten(await LoadEntries());

        var resolvedIcons = new Dictionary<Service, (string? Icon, string? ImageUrl)>();
        foreach (var s in services.Where(s => !handWritten.ContainsKey(s)))
        {
            resolvedIcons[s] = await _iconResolver.Resolve(s, this);
        }

        // Hand-written entries go back as read; only DSM's own are rebuilt.
        var output = services
            .GroupBy(s => string.IsNullOrEmpty(s.Category) ? UncategorizedGroup : ResolveCategoryDisplayName(s.Category!))
            .OrderBy(g => g.Key)
            .Select(g => new Dictionary<string, List<Dictionary<string, object>>>
            {
                [g.Key] = g.OrderBy(s => s.Name)
                    .Select(s => handWritten.TryGetValue(s, out var node)
                        ? new Dictionary<string, object> { [s.Name] = node }
                        : ToEntryMap(s, resolvedIcons[s], _config.EnableStatusMonitoring, widgets, _logger))
                    .ToList()
            })
            .ToList();

        var serializer = CreateSerializer();
        var sw = new StringWriter();
        serializer.Serialize(sw, output);
        var servicesChanged = await YamlFileWriter.WriteIfChanged(ServicesFilePath, sw.ToString());

        var settingsChanged = await UpdateSettingsLayoutIcons(services);
        return servicesChanged || settingsChanged;
    }

    private async Task<bool> UpdateSettingsLayoutIcons(List<Service> services)
    {
        var distinctGroups = services
            .Select(s => string.IsNullOrEmpty(s.Category) ? UncategorizedGroup : ResolveCategoryDisplayName(s.Category!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groupIcons = new List<(string Name, string Icon)>();
        foreach (var name in distinctGroups)
        {
            var rawIcon = _serviceDefaultOptions.FindCategory(name)?.Icon;
            var (icon, imageUrl) = await _iconResolver.ResolveIcon(rawIcon, this);
            var resolved = imageUrl ?? icon;
            if (!string.IsNullOrEmpty(resolved))
            {
                groupIcons.Add((name, resolved));
            }
        }
        if (groupIcons.Count == 0) return false;

        // settings.yaml is edited as YAML nodes, so every other value keeps its text and quoting.
        var settings = await LoadSettings();
        var layout = GetOrCreateLayoutMap(settings);

        foreach (var (name, icon) in groupIcons)
        {
            if (YamlNodes.Child(layout, name) is YamlMappingNode existingMap)
            {
                YamlNodes.Set(existingMap, IconKey, new YamlScalarNode(icon));
            }
            else
            {
                YamlNodes.Set(layout, name, new YamlMappingNode { { IconKey, icon } });
            }
        }

        var serializer = CreateSerializer();
        var sw = new StringWriter();
        serializer.Serialize(sw, settings);
        return await YamlFileWriter.WriteIfChanged(SettingsFilePath, sw.ToString());
    }

    private async Task<YamlMappingNode> LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new YamlMappingNode();
        }
        var text = await File.ReadAllTextAsync(SettingsFilePath);
        var parsed = YamlNodes.Load(text);
        return parsed is null ? new YamlMappingNode() : (YamlMappingNode)parsed;
    }

    private YamlMappingNode GetOrCreateLayoutMap(YamlMappingNode settings)
    {
        var existing = YamlNodes.Child(settings, LayoutKey);
        if (existing is YamlMappingNode asMap)
        {
            return asMap;
        }
        if (existing is not null)
        {
            _logger.LogWarning("Homepage settings.yaml 'layout' was not a map ({Type}); replacing it.",
                existing.NodeType);
        }

        var layout = new YamlMappingNode();
        YamlNodes.Set(settings, LayoutKey, layout);
        return layout;
    }

    private string ResolveCategoryDisplayName(string category)
    {
        if (_serviceDefaultOptions.Categories.TryGetValue(category, out var c)
            && !string.IsNullOrEmpty(c.Name))
            return c.Name;
        return TitleCaseCategory(category);
    }

    private static string TitleCaseCategory(string category)
    {
        var titled = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(category);
        return LetterAfterDigit.Replace(titled, m => m.Value.ToUpperInvariant());
    }

    private static Dictionary<string, object> ToEntryMap(
        Service service,
        (string? Icon, string? ImageUrl) resolvedIcon,
        bool enableStatusMonitoring,
        IReadOnlyDictionary<string, List<HomepageServiceWidgetEntry>> widgets,
        ILogger logger)
    {
        var icon = !string.IsNullOrEmpty(resolvedIcon.ImageUrl) ? resolvedIcon.ImageUrl : resolvedIcon.Icon;
        var w = FindWidget(widgets, service);
        var hasWidget = w is not null && (w.Widgets is { Count: > 0 } || w.Widget is not null);
        var entry = new HomepageServiceEntry
        {
            Href = service.Url,
            Icon = string.IsNullOrEmpty(icon) ? null : icon,
            Server = service.Hostname,
            SiteMonitor = enableStatusMonitoring && !hasWidget ? service.Url : null,
            Autogenerated = service.Autogenerated,
            ServiceDefaultsName = service.ServiceDefaultsName,
            ProviderId = service.ProviderId
        };
        if (w is not null)
        {
            if (w.Widgets is { Count: > 0 })
            {
                entry.Widgets = w.Widgets;
                if (w.Widget is not null)
                {
                    logger.LogWarning(
                        "Homepage widget source has both 'widget' and 'widgets' for service '{Name}'; using 'widgets'.",
                        service.Name);
                }
            }
            else if (w.Widget is not null)
            {
                entry.Widget = w.Widget;
            }
        }
        return new Dictionary<string, object> { [service.Name] = entry };
    }

    private static HomepageServiceWidgetEntry? FindWidget(
        IReadOnlyDictionary<string, List<HomepageServiceWidgetEntry>> widgets,
        Service service)
    {
        var names = new List<string>();
        if (!string.IsNullOrEmpty(service.Name)) names.Add(service.Name);
        if (!string.IsNullOrEmpty(service.ServiceDefaultsName) &&
            !string.Equals(service.ServiceDefaultsName, service.Name, StringComparison.OrdinalIgnoreCase))
        {
            names.Add(service.ServiceDefaultsName);
        }

        if (!string.IsNullOrEmpty(service.Hostname))
        {
            foreach (var name in names)
            {
                if (!widgets.TryGetValue(name, out var list)) continue;
                var match = list.FirstOrDefault(e =>
                    string.Equals(e.Server, service.Hostname, StringComparison.OrdinalIgnoreCase));
                if (match is not null) return match;
            }
        }

        foreach (var name in names)
        {
            if (!widgets.TryGetValue(name, out var list)) continue;
            var match = list.FirstOrDefault(e => string.IsNullOrEmpty(e.Server));
            if (match is not null) return match;
        }
        return null;
    }

    private static (string? icon, string? imageUrl) GetIconOrImageUrl(string? icon)
    {
        if (string.IsNullOrEmpty(icon)) return (null, null);
        return icon.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? (null, icon) : (icon, null);
    }

    private async Task<Dictionary<string, List<HomepageServiceWidgetEntry>>> LoadWidgetEntries()
    {
        var result = new Dictionary<string, List<HomepageServiceWidgetEntry>>(StringComparer.OrdinalIgnoreCase);
        var path = _config.SourceHomepageServiceWidgetsFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return result;
        }
        if (!File.Exists(path))
        {
            _logger.LogWarning("Homepage service widgets file '{Path}' does not exist; no widgets will be applied.", path);
            return result;
        }

        var text = await File.ReadAllTextAsync(path);
        var entries = CreateDeserializer()
            .Deserialize<List<Dictionary<string, HomepageServiceWidgetEntry>>>(text);
        if (entries is null)
        {
            return result;
        }

        foreach (var map in entries)
        {
            if (map is null) continue;
            foreach (var (name, entry) in map)
            {
                if (string.IsNullOrWhiteSpace(name) || entry is null) continue;
                if (!result.TryGetValue(name, out var list))
                {
                    list = [];
                    result[name] = list;
                }
                var existing = list.FindIndex(e =>
                    string.Equals(e.Server ?? string.Empty, entry.Server ?? string.Empty,
                                  StringComparison.OrdinalIgnoreCase));
                if (existing >= 0)
                {
                    _logger.LogWarning(
                        "Homepage service widgets file has duplicate entry for service '{Name}' (server '{Server}'); last-wins.",
                        name, entry.Server ?? "<none>");
                    list[existing] = entry;
                }
                else
                {
                    list.Add(entry);
                }
            }
        }
        return result;
    }

    // Every entry in services.yaml as the Service it reads as, paired with its YAML node. The typed
    // parse and the node tree come from the same text, so they line up group by group and entry
    // by entry.
    private async Task<List<(Service Service, YamlMappingNode Node)>> LoadEntries()
    {
        var path = ServicesFilePath;
        if (!File.Exists(path))
        {
            _logger.LogWarning("Homepage config file '{Path}' does not exist; using empty config.", path);
            return [];
        }

        var text = await File.ReadAllTextAsync(path);
        var groups = CreateDeserializer()
            .Deserialize<List<Dictionary<string, List<Dictionary<string, HomepageServiceEntry>>>>>(text) ?? [];
        var groupNodes = (YamlNodes.Load(text) as YamlSequenceNode)?.Children ?? [];

        var entries = new List<(Service, YamlMappingNode)>();
        for (var g = 0; g < groups.Count; g++)
        {
            if (groups[g] is null) continue;
            var groupNode = (YamlMappingNode)groupNodes[g];
            foreach (var (groupName, groupEntries) in groups[g])
            {
                if (groupEntries is null) continue;
                var entryNodes = ((YamlSequenceNode)YamlNodes.Child(groupNode, groupName)!).Children;
                for (var e = 0; e < groupEntries.Count; e++)
                {
                    if (groupEntries[e] is null) continue;
                    var entryNode = (YamlMappingNode)entryNodes[e];
                    foreach (var (serviceName, entry) in groupEntries[e])
                    {
                        var node = (YamlMappingNode)YamlNodes.Child(entryNode, serviceName)!;
                        entries.Add((ToService(groupName, serviceName, entry), node));
                    }
                }
            }
        }
        return entries;
    }

    private static Service ToService(string groupName, string serviceName, HomepageServiceEntry entry)
    {
        var (icon, imageUrl) = GetIconOrImageUrl(entry.Icon);
        return new Service(
            serviceName,
            entry.Href,
            groupName,
            icon,
            imageUrl,
            entry.Server,
            false,
            entry.Autogenerated,
            entry.ServiceDefaultsName,
            entry.ProviderId);
    }

    private static IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    private static ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
    }
}
