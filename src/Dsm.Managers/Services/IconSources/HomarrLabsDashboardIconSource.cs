namespace Dsm.Managers.Services.IconSources;

public class HomarrLabsDashboardIconSource : JsDelivrIconSource
{
    public const string ClientName = "homarrlabs";

    public HomarrLabsDashboardIconSource(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public override DashboardIconSourceType Type => DashboardIconSourceType.HomarrLabs;
    public override string Prefix => "hl-";
    protected override string GitHubSlug => "homarr-labs/dashboard-icons";
    protected override string HttpClientName => ClientName;
}
