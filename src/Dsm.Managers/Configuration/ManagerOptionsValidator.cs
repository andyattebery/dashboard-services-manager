using Microsoft.Extensions.Options;

namespace Dsm.Managers.Configuration;

public sealed class ManagerOptionsValidator : IValidateOptions<ManagerOptions>
{
    public ValidateOptionsResult Validate(string? name, ManagerOptions options)
    {
        var failures = new List<string>();
        var seen = new HashSet<string>();
        foreach (var (config, i) in options.DashboardManagers.Select((c, i) => (c, i)))
        {
            var key = config.DashboardManagerType.ToString();
            if (!seen.Add(key))
            {
                failures.Add(
                    $"DashboardManagers[{i}]: duplicate {nameof(DashboardManagerConfig.DashboardManagerType)} '{key}'. " +
                    "Per-entry disambiguation (e.g. a name field) is not yet supported.");
            }
            if (string.IsNullOrWhiteSpace(config.DashboardConfigDirectoryPath))
            {
                failures.Add(
                    $"DashboardManagers[{i}] ({key}): {nameof(DashboardManagerConfig.DashboardConfigDirectoryPath)} is required.");
            }
        }
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
