namespace Dsm.Managers.Services.IconSources;

public enum DashboardIconSourceType
{
    HomarrLabs
}

public interface IDashboardIconSource
{
    DashboardIconSourceType Type { get; }
    string Prefix { get; }
    Task<string?> GetIconUrl(string iconName);
}
