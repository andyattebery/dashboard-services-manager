using System.Globalization;
using System.Text.RegularExpressions;

namespace Dcm.Provider.ServicesProviders;
public static class ServicesProviderUtilities
{
    public static string GetFormattedServiceName(string originalName)
    {
        var formattedName = Regex.Replace(originalName, @"[_-]", " ");
        formattedName = Regex.Replace(formattedName, @"db", "DB");
        formattedName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedName);
        return formattedName;
    }
}