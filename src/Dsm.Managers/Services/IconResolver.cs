using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Managers.Services.IconSources;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Services;

public class IconResolver
{
    private readonly ServiceDefaultOptions _serviceDefaultOptions;
    private readonly Dictionary<DashboardIconSourceType, IDashboardIconSource> _iconSources;

    public IconResolver(
        IOptions<ServiceDefaultOptions> defaultOptions,
        IEnumerable<IDashboardIconSource> iconSources)
    {
        _serviceDefaultOptions = defaultOptions.Value;
        _iconSources = iconSources.ToDictionary(s => s.Type);
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
        if (!string.IsNullOrEmpty(service.Icon))
        {
            var match = _iconSources.Values.FirstOrDefault(
                s => service.Icon.StartsWith(s.Prefix, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                var iconName = service.Icon[match.Prefix.Length..];

                // Explicit prefix from the user — they vouch for the icon, so no probe needed
                // when the manager natively handles the source.
                if (manager.NativeIconSourcePrefixes.TryGetValue(match.Type, out var nativePrefix))
                {
                    return (nativePrefix + iconName, null);
                }

                var (url, _) = await match.GetIconUrl(iconName);
                if (!string.IsNullOrEmpty(url))
                {
                    return (null, url);
                }
                // CDN miss — fall through so a defaults-supplied ImageUrl can take over.
            }
        }

        // 2. Pre-resolved image URL (e.g. ImagePath turned into a URL by the factory).
        if (!string.IsNullOrEmpty(service.ImageUrl))
        {
            return (null, service.ImageUrl);
        }

        // 3. Icon string verbatim — no prefix, or prefix that missed the CDN.
        if (!string.IsNullOrEmpty(service.Icon))
        {
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
                return (nativePrefix + matchedName, null);
            }

            return (null, url);
        }

        return (null, null);
    }
}
