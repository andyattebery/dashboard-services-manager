using Microsoft.Extensions.Logging;

namespace Dsm.Managers.Services.IconSources;

public class HomarrLabsDashboardIconSource : JsDelivrIconSource
{
    public const string ClientName = "homarrlabs";

    public HomarrLabsDashboardIconSource(IHttpClientFactory httpClientFactory, ILogger<HomarrLabsDashboardIconSource> logger)
        : base(httpClientFactory, logger) { }

    public override DashboardIconSourceType Type => DashboardIconSourceType.HomarrLabs;
    public override string Prefix => "hl-";
    protected override string BaseUrl => "https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/";
    protected override string Extension => "png";
    protected override string HttpClientName => ClientName;
}
