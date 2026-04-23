using System.Globalization;
using System.Text.RegularExpressions;

namespace Dsm.Providers.ServicesProviders;
public static class ServicesProviderUtilities
{
    private static readonly Regex UnderscoreOrHyphenRegex = new(@"[_-]");
    private static readonly Regex DbWordRegex = new(@"\bdb\b", RegexOptions.IgnoreCase);

    public static string GetFormattedServiceName(string originalName)
    {
        var formattedName = UnderscoreOrHyphenRegex.Replace(originalName, " ");
        formattedName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(formattedName);
        formattedName = DbWordRegex.Replace(formattedName, "DB");
        return formattedName;
    }
}
