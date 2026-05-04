using Microsoft.Extensions.Logging;

namespace Dsm.Managers.Services.IconSources;

public class MaterialDesignIconsDashboardIconSource : JsDelivrIconSource
{
    public const string ClientName = "materialdesignicons";

    public MaterialDesignIconsDashboardIconSource(IHttpClientFactory httpClientFactory, ILogger<MaterialDesignIconsDashboardIconSource> logger)
        : base(httpClientFactory, logger) { }

    public override DashboardIconSourceType Type => DashboardIconSourceType.MaterialDesignIcons;
    public override string Prefix => "mdi-";
    protected override string BaseUrl => "https://cdn.jsdelivr.net/npm/@mdi/svg@latest/svg/";
    protected override string Extension => "svg";
    protected override string HttpClientName => ClientName;
}
