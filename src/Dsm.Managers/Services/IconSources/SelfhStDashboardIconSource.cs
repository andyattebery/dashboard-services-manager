namespace Dsm.Managers.Services.IconSources;

public class SelfhStDashboardIconSource : JsDelivrIconSource
{
    public const string ClientName = "selfhst";

    public SelfhStDashboardIconSource(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public override DashboardIconSourceType Type => DashboardIconSourceType.SelfhSt;
    public override string Prefix => "sh-";
    protected override string BaseUrl => "https://cdn.jsdelivr.net/gh/selfhst/icons/png/";
    protected override string Extension => "png";
    protected override string HttpClientName => ClientName;
}
