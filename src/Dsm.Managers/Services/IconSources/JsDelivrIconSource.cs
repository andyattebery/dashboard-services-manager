using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Dsm.Managers.Services.IconSources;

public abstract class JsDelivrIconSource : IDashboardIconSource
{
    private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromDays(7);

    private readonly ConcurrentDictionary<string, (string? Url, string? MatchedName, DateTime CachedAt)> _cache = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    protected JsDelivrIconSource(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public abstract DashboardIconSourceType Type { get; }
    public abstract string Prefix { get; }
    protected abstract string BaseUrl { get; }
    protected abstract string Extension { get; }
    protected abstract string HttpClientName { get; }

    public async Task<(string? Url, string? MatchedName)> GetIconUrl(string iconName)
    {
        if (_cache.TryGetValue(iconName, out var cached) &&
            (cached.Url is not null || DateTime.UtcNow - cached.CachedAt < NegativeCacheTtl))
        {
            _logger.LogDebug("Icon cache hit for '{IconName}' on '{SourceType}': '{Url}'",
                iconName, Type, cached.Url ?? "<negative>");
            return (cached.Url, cached.MatchedName);
        }

        var lowerCaseName = iconName.ToLowerInvariant();
        var potentialIconNames = new[]
        {
            lowerCaseName.Replace(" ", string.Empty),
            lowerCaseName.Replace(" ", "-"),
            lowerCaseName.Replace(".", "-")
        };

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        for (var i = 0; i < potentialIconNames.Length; i++)
        {
            var potentialIconName = potentialIconNames[i];
            var iconUrl = $"{BaseUrl}{potentialIconName}.{Extension}";
            using var request = new HttpRequestMessage(HttpMethod.Head, iconUrl);
            using var response = await httpClient.SendAsync(request);
            _logger.LogDebug("CDN HEAD '{Url}' for icon '{IconName}' (variant {Variant}/3): {StatusCode}",
                iconUrl, iconName, i + 1, (int)response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                _cache[iconName] = (iconUrl, potentialIconName, DateTime.UtcNow);
                return (iconUrl, potentialIconName);
            }
        }

        _cache[iconName] = (null, null, DateTime.UtcNow);
        return (null, null);
    }
}
