using System.Globalization;
using System.Text.RegularExpressions;
using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers.Dashy;
using Dsm.Shared.Extensions;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dsm.Managers.DashboardManagers;

public class DashyDashboardManager : IDashboardManager
{
    private readonly ServiceDefaultOptions _serviceDefaultOptions;
    private readonly ManagerOptions _managerOptions;

    public DashyDashboardManager(
        IOptions<ManagerOptions> managerOptions,
        IOptions<ServiceDefaultOptions> defaultOptions)
    {
        _serviceDefaultOptions = defaultOptions.Value;
        _managerOptions = managerOptions.Value;
    }

    public async Task<List<Service>> ListServices()
    {
        var dashyConfig = await LoadDashyConfig<DashyConfig>();
        var services = new List<Service>();
        foreach (var dashySection in dashyConfig.Sections)
        {
            foreach (var dashyItem in dashySection.Items)
            {
                var (icon, imageUrl) = GetIconOrImageUrl(dashyItem.Icon);
                var hostname = GetHostnameFromTags(dashyItem.Tags);
                var service = new Service(dashyItem.Title, dashyItem.Url, dashySection.Name, icon,
                    imageUrl, hostname, false);
                services.Add(service);
            }
        }

        return services;
    }

    public async Task UpdateWithNewServices(List<Service> services)
    {
        var dashySections = CreateDashySections(services);

        var serializer = new SerializerBuilder().Build();
        var deserializer = CreateDeserializer();
        var dashySectionsYaml = serializer.Serialize(dashySections);
        var dashySectionsObject = deserializer.Deserialize<object>(dashySectionsYaml);
        var dashyConfigObject = await LoadDashyConfig<Dictionary<object, object>>();

        if (dashyConfigObject.ContainsKey("sections"))
        {
            dashyConfigObject["sections"] = dashySectionsObject;
        }
        else
        {
            dashyConfigObject.Add("sections", dashySectionsObject);
        }

        using (var textWriter = File.CreateText(_managerOptions.DashboardConfigFilePath))
        {
            serializer.Serialize(textWriter, dashyConfigObject);
        }
    }

    private static (string? icon, string? imageUrl) GetIconOrImageUrl(string icon)
    {
        return Regex.IsMatch(icon, @"^http", RegexOptions.IgnoreCase) ? (null, icon) : (icon, null);
    }

    private static string? GetHostnameFromTags(List<string> tags)
    {
        foreach (var tag in tags)
        {
            var hostMatch = Regex.Match(tag, @"^host=(.*)$");
            if (hostMatch.Success &&
                !string.IsNullOrEmpty(hostMatch.Groups[1].Value))
            {
                return hostMatch.Groups[1].Value;
            }
        }

        return null;
    }

    private List<DashySection> CreateDashySections(List<Service> services)
    {
        var sectionNameToItemsMapping = new Dictionary<string, List<DashyItem>>();
        foreach (var service in services)
        {
            var dashyItem = DashyItem.Create(service);
            sectionNameToItemsMapping.AddToLookup(service.Category.ToLower(), dashyItem);
        }

        var dashySections = sectionNameToItemsMapping
            .Select(kvp => CreateDashySection(kvp.Key, kvp.Value))
            .ToList();
        return dashySections;
    }

    private DashySection CreateDashySection(string name, List<DashyItem> dashyItems)
    {
        string? sectionIcon = null;

        var dashySectionName = string.IsNullOrEmpty(name)
            ? "Uncategorized"
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);

        if (_serviceDefaultOptions.Categories.TryGetValue(dashySectionName.ToLower(), out var defaultCategoryConfig))
        {
            sectionIcon = defaultCategoryConfig.Icon;
        }

        var dashySection = new DashySection(dashySectionName, sectionIcon, dashyItems);
        return dashySection;
    }

    private async Task<T> LoadDashyConfig<T>()
        where T : new()
    {
        if (!File.Exists(_managerOptions.DashboardConfigFilePath))
        {
            throw new FileNotFoundException(
                $"{nameof(ManagerOptions)}.{nameof(ManagerOptions.DashboardConfigFilePath)}: {_managerOptions.DashboardConfigFilePath} does not exist.");
        }

        var deserializer = CreateDeserializer();
        return await Task.Run(() =>
        {
            using (var reader = File.OpenText(_managerOptions.DashboardConfigFilePath))
            {
                var dashyConfig = deserializer.Deserialize<T>(reader);
                return dashyConfig ?? new T();
            }
        });
    }

    private static IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }
}