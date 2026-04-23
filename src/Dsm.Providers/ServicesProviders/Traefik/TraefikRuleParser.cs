using System.Text.RegularExpressions;

namespace Dsm.Providers.ServicesProviders.Traefik;

public static class TraefikRuleParser
{
    private static readonly Regex HostRegex = new(@"Host\(([^)]+)\)");

    public static string? ExtractFirstHost(string? rule)
    {
        if (string.IsNullOrEmpty(rule))
        {
            return null;
        }

        var match = HostRegex.Match(rule);
        if (!match.Success || string.IsNullOrEmpty(match.Groups[1].Value))
        {
            return null;
        }

        var host = match.Groups[1].Value.Replace("`", "").Split(",").FirstOrDefault();
        return string.IsNullOrEmpty(host) ? null : host;
    }

    public static string BuildUrl(string host, bool isHttps)
    {
        return isHttps ? $"https://{host}" : $"http://{host}";
    }
}
