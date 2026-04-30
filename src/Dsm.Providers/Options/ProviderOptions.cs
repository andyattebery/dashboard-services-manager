using System.ComponentModel.DataAnnotations;

namespace Dsm.Providers.Options;

public sealed class ProviderOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string ApiUrl { get; set; }

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(60);

    [MinLength(1)]
    public required List<ServicesProviderConfig> ServicesProviders { get; set; }
}
