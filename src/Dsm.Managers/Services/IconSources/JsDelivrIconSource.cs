using System.Collections.Concurrent;

namespace Dsm.Managers.Services.IconSources;

public abstract class JsDelivrIconSource : IDashboardIconSource
{
    private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromDays(7);

    private readonly ConcurrentDictionary<string, (string? Url, string? MatchedName, DateTime CachedAt)> _cache = new();
    private readonly IHttpClientFactory _httpClientFactory;

    protected JsDelivrIconSource(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
        foreach (var potentialIconName in potentialIconNames)
        {
            var iconUrl = $"{BaseUrl}{potentialIconName}.{Extension}";
            using var request = new HttpRequestMessage(HttpMethod.Head, iconUrl);
            using var response = await httpClient.SendAsync(request);
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
