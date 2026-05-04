using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Services;

public class IconResolver
{
    private readonly ServiceDefaultOptions _serviceDefaultOptions;
    private readonly Dictionary<DashboardIconSourceType, IDashboardIconSource> _iconSources;
    private readonly ILogger<IconResolver> _logger;

    public IconResolver(
        IOptions<ServiceDefaultOptions> defaultOptions,
        IEnumerable<IDashboardIconSource> iconSources,
        ILogger<IconResolver> logger)
    {
        _serviceDefaultOptions = defaultOptions.Value;
        _iconSources = iconSources.ToDictionary(s => s.Type);
        _logger = logger;
    }

    /// <summary>
    /// Resolves a service's icon to a (Icon, ImageUrl) pair for the given dashboard manager.
    /// Native-prefix matches pass through without a CDN probe; non-native matches probe the CDN.
    /// Precedence (mirrors the pre-refactor factory behaviour):
    ///   1. Icon with a registered prefix — native pass-through, or CDN probe; on hit, use the
    ///      result. On miss, fall through.
    ///   2. ImageUrl (typically from <see cref="ServiceConfig.ImagePath"/> resolved by the factory).
    ///   3. Icon string verbatim (no prefix, or prefix probe-miss).
    ///   4. FallbackIconSourceProviders chain — probed in order; native sources stamp the
    ///      prefix-form matched-name, non-native ones use the URL.
    /// </summary>
    public async Task<(string? Icon, string? ImageUrl)> Resolve(Service service, IDashboardManager manager)
    {
        // 1. Icon with a registered prefix wins outright when it can be resolved.
        var prefixResult = await ResolvePrefix(service.Icon, manager);
        if (prefixResult is { } hit)
        {
            _logger.LogDebug("Icon for '{ServiceName}' on '{DashboardManager}': resolved via prefix match to icon='{Icon}', imageUrl='{ImageUrl}'",
                service.Name, manager.Type, hit.Icon, hit.ImageUrl);
            return hit;
        }

        // 2. Pre-resolved image URL (e.g. ImagePath turned into a URL by the factory).
        if (!string.IsNullOrEmpty(service.ImageUrl))
        {
            _logger.LogDebug("Icon for '{ServiceName}' on '{DashboardManager}': using pre-resolved image URL '{ImageUrl}'",
                service.Name, manager.Type, service.ImageUrl);
            return (null, service.ImageUrl);
        }

        // 3. Icon string verbatim — no prefix, or prefix that missed the CDN.
        if (!string.IsNullOrEmpty(service.Icon))
        {
            _logger.LogDebug("Icon for '{ServiceName}' on '{DashboardManager}': using verbatim icon '{Icon}' (no prefix or prefix CDN miss)",
                service.Name, manager.Type, service.Icon);
            return (service.Icon, null);
        }

        // 4. Both empty: walk the fallback chain. Probe every entry, even when the manager
        // natively supports it — without the probe we'd stamp a prefix-form name that might
        // 404 in the dashboard's own renderer.
        var lookupName = !string.IsNullOrEmpty(service.ServiceDefaultsName)
            ? service.ServiceDefaultsName
            : service.Name;

        foreach (var type in _serviceDefaultOptions.FallbackIconSourceProviders)
        {
            if (!_iconSources.TryGetValue(type, out var source)) continue;

            var (url, matchedName) = await source.GetIconUrl(lookupName);
            if (string.IsNullOrEmpty(url)) continue;

            if (manager.NativeIconSourcePrefixes.TryGetValue(type, out var nativePrefix))
            {
                _logger.LogDebug("Icon for '{ServiceName}' on '{DashboardManager}': resolved via fallback '{SourceType}' to native '{Icon}'",
                    service.Name, manager.Type, type, nativePrefix + matchedName);
                return (nativePrefix + matchedName, null);
            }

            _logger.LogDebug("Icon for '{ServiceName}' on '{DashboardManager}': resolved via fallback '{SourceType}' to URL '{Url}'",
                service.Name, manager.Type, type, url);
            return (null, url);
        }

        _logger.LogWarning("Icon for '{ServiceName}' on '{DashboardManager}': no match across all sources; rendering plaintext name",
            service.Name, manager.Type);
        return (null, null);
    }

    /// <summary>
    /// Resolves a standalone icon string (e.g. a Dashy section icon or a Homepage layout
    /// icon) against a target dashboard manager. Same prefix-translation and CDN-probe
    /// behaviour as service icons, minus the fallback chain — categories with no icon stay
    /// silent.
    /// </summary>
    public async Task<(string? Icon, string? ImageUrl)> ResolveIcon(string? icon, IDashboardManager manager)
    {
        var prefixResult = await ResolvePrefix(icon, manager);
        if (prefixResult is { } hit)
        {
            return hit;
        }

        return string.IsNullOrEmpty(icon) ? (null, null) : (icon, null);
    }

    /// <summary>
    /// Returns a non-null tuple iff the icon matched a registered prefix AND the resolution
    /// produced a definitive answer (native pass-through or CDN hit). Returns null on
    /// no-prefix-match or prefix-with-CDN-miss, signalling the caller to fall through.
    /// </summary>
    private async Task<(string? Icon, string? ImageUrl)?> ResolvePrefix(string? icon, IDashboardManager manager)
    {
        if (string.IsNullOrEmpty(icon)) return null;

        var match = _iconSources.Values.FirstOrDefault(
            s => icon.StartsWith(s.Prefix, StringComparison.OrdinalIgnoreCase));
        if (match is null) return null;

        var iconName = icon[match.Prefix.Length..];

        if (manager.NativeIconSourcePrefixes.TryGetValue(match.Type, out var nativePrefix))
        {
            return (nativePrefix + iconName, null);
        }

        var (url, _) = await match.GetIconUrl(iconName);
        if (!string.IsNullOrEmpty(url))
        {
            return (null, url);
        }

        return null; // CDN miss — caller falls through.
    }
}
