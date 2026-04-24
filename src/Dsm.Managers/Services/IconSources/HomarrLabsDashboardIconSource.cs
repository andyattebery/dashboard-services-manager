using System.Collections.Concurrent;

namespace Dsm.Managers.Services.IconSources;

public class HomarrLabsDashboardIconSource : IDashboardIconSource
{
    public const string HttpClientName = "homarrlabs";
    private const string BaseUrl = "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/";
    private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromHours(1);
    private static readonly ConcurrentDictionary<string, (string? Url, DateTime CachedAt)> Cache = new();

    private readonly IHttpClientFactory _httpClientFactory;

    public HomarrLabsDashboardIconSource(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public DashboardIconSourceType Type => DashboardIconSourceType.HomarrLabs;

    public string Prefix => "hl-";

    public async Task<string?> GetIconUrl(string iconName)
    {
        if (Cache.TryGetValue(iconName, out var cached) &&
            (cached.Url is not null || DateTime.UtcNow - cached.CachedAt < NegativeCacheTtl))
        {
            return cached.Url;
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
            var iconUrl = $"{BaseUrl}{potentialIconName}.png";
            using var request = new HttpRequestMessage(HttpMethod.Head, iconUrl);
            using var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Cache[iconName] = (iconUrl, DateTime.UtcNow);
                return iconUrl;
            }
        }

        Cache[iconName] = (null, DateTime.UtcNow);
        return null;
    }
}
