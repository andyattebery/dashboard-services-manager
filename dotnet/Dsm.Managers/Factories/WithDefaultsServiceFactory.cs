using Dsm.Managers.Configuration;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Services;

public class ServicesFactory
{
    private readonly DefaultOptions _defaultOptions;

    public ServicesFactory(IOptions<DefaultOptions> defaultOptions)
    {
        _defaultOptions = defaultOptions.Value;
    }

    public Service CreateWithDefaults(Service service)
    {
        if (!_defaultOptions.Services.TryGetValue(service.Name.ToLower(), out var defaultServiceConfig))
        {
            return service;
        }
        
        return new Service(
            service.Name,
            service.Url,
            service.Category ?? defaultServiceConfig.Category,
            service.Icon ?? defaultServiceConfig.Icon,
            service.ImageUrl ?? defaultServiceConfig.ImagePath,
            service.Hostname,
            service.Ignore);
    }
}