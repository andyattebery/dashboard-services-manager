using System.Text.Json;
using System.Text.Json.Serialization;
using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dsm.Managers;

// What DSM knows: the latest copies each provider source (ProviderId) posted, including the ones
// a dashboard never shows, because they are ignored or have no URL. ServiceMerger turns them into
// dashboard entries; the dashboard files are output, read back only for hand-written entries.
//
// Persisted to /data/state.json, so a manager restart keeps the copies a dashboard can't hold.
// Without a writable /data the state stays in memory (one warning). With no state file yet (first
// run, or it was lost), the store seeds itself once from the first dashboard's DSM-owned entries.
public sealed class ProviderServiceStore
{
    public const string DefaultStateFilePath = "/data/state.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    private readonly string _stateFilePath;
    private readonly ManagerOptions _managerOptions;
    private readonly DashboardManagerFactory _dashboardManagerFactory;
    private readonly ILogger<ProviderServiceStore> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly object _copiesLock = new();
    private Dictionary<string, List<Service>> _copies = new(StringComparer.Ordinal);
    private volatile bool _loaded;
    private bool _persist;

    public ProviderServiceStore(
        IOptions<ManagerOptions> managerOptions,
        DashboardManagerFactory dashboardManagerFactory,
        ILogger<ProviderServiceStore> logger)
        : this(DefaultStateFilePath, managerOptions, dashboardManagerFactory, logger)
    {
    }

    internal ProviderServiceStore(
        string stateFilePath,
        IOptions<ManagerOptions> managerOptions,
        DashboardManagerFactory dashboardManagerFactory,
        ILogger<ProviderServiceStore> logger)
    {
        _stateFilePath = stateFilePath;
        _managerOptions = managerOptions.Value;
        _dashboardManagerFactory = dashboardManagerFactory;
        _logger = logger;
    }

    public IReadOnlyList<string> ProviderIds
    {
        get { lock (_copiesLock) return _copies.Keys.ToList(); }
    }

    public List<Service> Copies
    {
        get { lock (_copiesLock) return _copies.Values.SelectMany(c => c).ToList(); }
    }

    public async Task EnsureLoaded()
    {
        if (_loaded) return;
        await _loadLock.WaitAsync();
        try
        {
            if (_loaded) return;
            var directory = Path.GetDirectoryName(_stateFilePath);
            _persist = string.IsNullOrEmpty(directory) || Directory.Exists(directory);
            if (!_persist)
            {
                _logger.LogWarning(
                    "State directory '{Directory}' doesn't exist; keeping provider state in memory, so a manager restart loses the copies dashboards can't hold (ignored or URL-less). Mount a directory there to keep them.",
                    directory);
            }
            var copies = _persist ? await Read() : null;
            copies ??= await Seed();
            lock (_copiesLock) _copies = copies;
            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    // Replaces the copies of every ProviderId in services; other ProviderIds keep theirs.
    public void Replace(IEnumerable<Service> services)
    {
        lock (_copiesLock)
        {
            foreach (var posted in services.Where(s => s.ProviderId is not null).GroupBy(s => s.ProviderId!))
            {
                _copies[posted.Key] = Sorted(posted);
            }
        }
    }

    public void Remove(string providerId)
    {
        lock (_copiesLock) _copies.Remove(providerId);
    }

    public async Task Save()
    {
        if (!_persist) return;
        string json;
        lock (_copiesLock)
        {
            json = JsonSerializer.Serialize(
                new StateFile(new SortedDictionary<string, List<Service>>(_copies, StringComparer.Ordinal)),
                JsonOptions);
        }
        try
        {
            await YamlFileWriter.WriteIfChanged(_stateFilePath, json);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _persist = false;
            _logger.LogWarning(e, "Can't write state file '{Path}'; keeping provider state in memory from now on.",
                _stateFilePath);
        }
    }

    private async Task<Dictionary<string, List<Service>>?> Read()
    {
        if (!File.Exists(_stateFilePath)) return null;
        try
        {
            var state = JsonSerializer.Deserialize<StateFile>(await File.ReadAllTextAsync(_stateFilePath), JsonOptions);
            if (state?.Providers is null) return null;
            return state.Providers.ToDictionary(p => p.Key, p => Sorted(p.Value), StringComparer.Ordinal);
        }
        catch (JsonException e)
        {
            _logger.LogWarning(e, "State file '{Path}' is unreadable; rebuilding it from the dashboards.", _stateFilePath);
            return null;
        }
    }

    // A one-time migration: the first dashboard's DSM-owned entries stand in for each source's copies
    // until that source posts again. Without it, the first POST after an upgrade would drop every
    // other source's entries until each one posted.
    private async Task<Dictionary<string, List<Service>>> Seed()
    {
        var firstDashboard = _managerOptions.DashboardManagers.FirstOrDefault();
        if (firstDashboard is null) return new Dictionary<string, List<Service>>(StringComparer.Ordinal);

        var manager = _dashboardManagerFactory.Create(firstDashboard);
        var copies = (await manager.ListServices())
            .Where(s => s.Autogenerated && s.ProviderId is not null)
            .GroupBy(s => s.ProviderId!)
            .ToDictionary(g => g.Key, Sorted, StringComparer.Ordinal);
        _logger.LogInformation("Seeded provider state from '{Path}': {Count} services from {ProviderCount} ProviderIds",
            manager.ConfigFilePath, copies.Values.Sum(c => c.Count), copies.Count);
        return copies;
    }

    // Stable order, so an unchanged POST serializes to identical bytes and the file isn't rewritten.
    private static List<Service> Sorted(IEnumerable<Service> services) =>
        services
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ThenBy(s => s.Hostname, StringComparer.Ordinal)
            .ThenBy(s => s.Url, StringComparer.Ordinal)
            .ToList();

    private sealed record StateFile(SortedDictionary<string, List<Service>> Providers);
}
