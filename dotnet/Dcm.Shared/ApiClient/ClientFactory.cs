using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Dcm.Shared.Options;

namespace Dcm.Shared.ApiClient;
public static class ClientFactory
{
    public static IDcmClient CreateDcmClient(IServiceProvider serviceProvider)
    {
        return RestService.For<IDcmClient>(CreateHttpClient(serviceProvider));
    }

    private static HttpClient CreateHttpClient(IServiceProvider serviceProvider)
    {
        var providerOptions = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
        return new HttpClient()
        {
            BaseAddress = new Uri(providerOptions.ApiUrl)
        };
    }
}