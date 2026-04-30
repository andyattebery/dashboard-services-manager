namespace Dsm.Managers.Services.IconSources;

public interface IDashboardIconSource
{
    DashboardIconSourceType Type { get; }
    string Prefix { get; }

    /// <summary>
    /// Probes the source for an icon. Returns the resolved CDN URL and the matched name
    /// variant (lowercase, hyphenated, etc.) so callers can stamp `<prefix><matched-name>` for
    /// dashboards that handle the source natively. Both fields are null on miss.
    /// </summary>
    Task<(string? Url, string? MatchedName)> GetIconUrl(string iconName);
}
