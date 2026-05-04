# Configuration: beyond the app-specific options

`manager-config.yaml` and `provider-config.yaml` aren't bespoke schemas — they feed the standard
.NET configuration tree. The fields documented in [managers.md](managers.md) and
[providers.md](providers.md) are the app-specific options you'll touch most often, but anything
you'd normally put in an `appsettings.json` file works in these YAML files too, just written in
YAML.

That means logging, log levels, Serilog sinks, request limits, allowed hosts, and any other
standard .NET setting can be configured in the same file you're already editing.

## Example: tweak log levels

```yaml
Serilog:
  MinimumLevel:
    Default: Information
    Override:
      Microsoft.AspNetCore: Warning
      Microsoft.Hosting.Lifetime: Information
```

Drop that anywhere in `manager-config.yaml` (or `provider-config.yaml`) alongside the
`ManagerOptions` / `ProviderOptions` block. Restart the container and the new levels apply.

## Environment variable equivalents

Every key also has an environment variable form, with `__` (double underscore) joining nested
keys and `__0`, `__1`, … indexing list entries. Both the unprefixed form and the `DSM_`-prefixed
form are read; values from env vars override the YAML file.

For example, the YAML above translates to:

```
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Override__Microsoft.AspNetCore=Warning
Serilog__MinimumLevel__Override__Microsoft.Hosting.Lifetime=Information
```

See the [Overriding via environment variables](../README.md#overriding-via-environment-variables)
section in the README for the same pattern applied to app-specific options.

## Optional API key

The manager API is unauthenticated by default. To require a shared secret on
`/dashboard-services`, set `ManagerOptions.ApiKey` in `manager-config.yaml` (or via
`DSM_ManagerOptions__ApiKey`) and the matching `ProviderOptions.ApiKey` on every provider.
When unset, the trusted-LAN behavior is preserved. `/health` is never gated. See the
[Optional API key](../README.md#optional-api-key) section in the README for the full example.

## More

- [loki.md](loki.md) — a worked example: enable a Grafana Loki sink purely through YAML.
- [managers.md](managers.md) — manager-specific options.
- [providers.md](providers.md) — provider-specific options.
