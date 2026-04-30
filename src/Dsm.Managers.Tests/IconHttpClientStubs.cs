using System.Net;
using Dsm.Managers.Services.IconSources;
using Microsoft.Extensions.DependencyInjection;

namespace Dsm.Managers.Tests;

/// <summary>
/// Replaces the primary message handler on the three icon-source named HttpClients with one that
/// returns 200 OK for every request. Lets tests run offline without flakes from real CDN probes
/// while keeping the production <c>service-defaults.yaml</c> (with its real fallback chain) in play.
/// </summary>
public static class IconHttpClientStubs
{
    public static IServiceCollection ConfigureOfflineIconHttpClients(this IServiceCollection services)
    {
        foreach (var name in new[]
        {
            HomarrLabsDashboardIconSource.ClientName,
            SelfhStDashboardIconSource.ClientName,
            MaterialDesignIconsDashboardIconSource.ClientName,
        })
        {
            services.AddHttpClient(name)
                .ConfigurePrimaryHttpMessageHandler(() => new OfflineIconCdnHandler());
        }
        return services;
    }

    private sealed class OfflineIconCdnHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
