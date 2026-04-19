using Dsm.Managers.Configuration;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Factories;

public class WithDefaultsServiceFactory
{
    private const string BaseWalkxcodeDashboardIconUrl = "https://cdn.jsdelivr.net/gh/walkxcode/dashboard-icons/png/";
    private static HttpClient _walkxcodeDashboardIconHttpClient = new HttpClient();

    private readonly ServiceDefaultOptions _serviceDefaultOptions;

    public WithDefaultsServiceFactory(IOptions<ServiceDefaultOptions> defaultOptions)
    {
        _serviceDefaultOptions = defaultOptions.Value;
    }

    public Service CreateWithDefaults(Service service)
    {
        if (!_serviceDefaultOptions.Services.TryGetValue(service.Name.ToLower(), out var defaultServiceConfig))
        {
            if (_serviceDefaultOptions.UseWalkxcodeDashboardIcons &&
                string.IsNullOrEmpty(service.Icon) &&
                string.IsNullOrEmpty(service.ImageUrl))
            {
                service.ImageUrl = GetWalkxcodeDashboardIconUrl(service.Name);
            }
            return service;
        }

        var icon = !string.IsNullOrEmpty(service.Icon) ? service.Icon : defaultServiceConfig.Icon;
        var imageUrl = !string.IsNullOrEmpty(service.ImageUrl) ? service.ImageUrl : defaultServiceConfig.ImagePath;
        if (_serviceDefaultOptions.UseWalkxcodeDashboardIcons &&
            string.IsNullOrEmpty(icon) &&
            string.IsNullOrEmpty(imageUrl))
        {
            imageUrl = GetWalkxcodeDashboardIconUrl(service.Name);
        }

        return new Service(
            service.Name,
            service.Url,
            !string.IsNullOrEmpty(service.Category) ? service.Category : defaultServiceConfig.Category,
            icon,
            imageUrl,
            service.Hostname,
            service.Ignore);
    }

    private static string? GetWalkxcodeDashboardIconUrl(string serviceName)
    {
        var lowerCaseServiceName = serviceName.ToLower();
        var potentialIconNames = new[]
        {
            lowerCaseServiceName.Replace(" ", string.Empty),
            lowerCaseServiceName.Replace(" ", "-"),
            lowerCaseServiceName.Replace(".", "-")
        };

        foreach (var potentialIconName in potentialIconNames)
        {
            var walkxcodeDashboardIconUrl = $"{BaseWalkxcodeDashboardIconUrl}{potentialIconName}.png";
            var request = new HttpRequestMessage(HttpMethod.Head, walkxcodeDashboardIconUrl);
            var response = _walkxcodeDashboardIconHttpClient.Send(request);
            if (response.IsSuccessStatusCode)
            {
                return walkxcodeDashboardIconUrl;
            }
        }

        return null;
    }
}