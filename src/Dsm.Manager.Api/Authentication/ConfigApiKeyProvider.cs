using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Dsm.Managers.Configuration;
using Microsoft.Extensions.Options;

namespace Dsm.Manager.Api.Authentication;

internal sealed class ConfigApiKeyProvider : IApiKeyProvider
{
    private readonly IOptions<ManagerOptions> _options;

    public ConfigApiKeyProvider(IOptions<ManagerOptions> options)
    {
        _options = options;
    }

    public Task<IApiKey?> ProvideAsync(string key)
    {
        var expected = _options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(key, expected, StringComparison.Ordinal))
        {
            return Task.FromResult<IApiKey?>(null);
        }
        return Task.FromResult<IApiKey?>(new ProviderApiKey(key));
    }

    private sealed record ProviderApiKey(string Key) : IApiKey
    {
        public string OwnerName => "Provider";
        public IReadOnlyCollection<Claim> Claims { get; } = [];
    }
}
