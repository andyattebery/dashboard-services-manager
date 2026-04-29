# Dsm.Providers + Dsm.Provider.App

Discover the services running in an environment and push them to the Manager API. The library
([Dsm.Providers](../src/Dsm.Providers/)) holds the provider implementations; the host
([Dsm.Provider.App](../src/Dsm.Provider.App/)) runs them on a schedule and does the HTTP call.

See [overview.md](overview.md) for how this fits into the larger system and
[managers.md](managers.md) for what happens after the POST lands.

## Provider abstraction

One interface, four implementations:

```csharp
public interface IServicesProvider
{
    Task<List<Service>> ListServices();
}
```

- [`DockerServicesProvider`](../src/Dsm.Providers/ServicesProviders/DockerServicesProvider.cs) — uses
  [Docker.DotNet](https://github.com/dotnet/Docker.DotNet) to list running containers and reads
  `<DockerLabelPrefix>.*` labels (see [Container label vocabulary](#container-label-vocabulary)
  below) to populate the [`Service`](../src/Dsm.Shared/Models/Service.cs) fields. Containers without
  a non-empty service `Url` (either set explicitly via the `url` label or recovered from a
  Traefik `Host(...)` router rule) are dropped.
- [`SwarmServicesProvider`](../src/Dsm.Providers/ServicesProviders/SwarmServicesProvider.cs) — same
  label vocabulary as Docker, but enumerates Swarm services. Strips the Docker stack namespace
  prefix from the service name (`mystack_jellyfin` → `jellyfin`) before label-driven Name overrides
  apply.
- [`YamlFileServicesProvider`](../src/Dsm.Providers/ServicesProviders/YamlFileServicesProvider.cs) —
  reads a flat YAML file at `ServicesProviderConfig.ServicesYamlFilePath`. Useful for bare-metal
  services that aren't containerized. The file deserializes with `UnderscoredNamingConvention`,
  so keys like `service_defaults_name` and `image_url` bind directly. If the file doesn't exist
  the provider logs a warning and returns an empty list (a configured-but-missing file isn't
  treated as a hard error so a single missing file doesn't take the worker down).
- [`TraefikServicesProvider`](../src/Dsm.Providers/ServicesProviders/TraefikServicesProvider.cs) —
  calls Traefik's [`GET /api/http/routers`](https://doc.traefik.io/traefik/reference/install-configuration/api-dashboard/#opt-apihttprouters)
  at `ServicesProviderConfig.TraefikApiUrl`. Filtering rules:
  - Skip routers whose `Status` isn't `enabled`.
  - Skip routers whose name ends with `@internal` (Traefik's own dashboard / API routers).
  - Skip routers whose `Rule` doesn't contain a `Host(...)` clause — DSM needs a hostname to
    build a service URL, and `PathPrefix`-only routers don't give us one.
  - Strip the `@provider` suffix (`my-router@docker` → `my-router`) and a trailing
    `-docker-compose` suffix (Traefik's auto-generated compose names) from the service name
    before emitting the `Service`.

The shared translation from container labels to a `Service` lives in
[`ContainerLabelServiceFactory`](../src/Dsm.Providers/Services/ContainerLabelServiceFactory.cs). The
`Host(...)` rule-parsing helper it shares with the Traefik provider lives in
[`TraefikRuleParser`](../src/Dsm.Providers/ServicesProviders/Traefik/TraefikRuleParser.cs).

Provider selection is done by the
[`ServicesProviderFactory`](../src/Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs), which
takes a [`ServicesProviderConfig`](../src/Dsm.Shared/Options/ProviderOptions.cs) entry from
`ProviderOptions.ServicesProviders` and builds the matching provider via
`ActivatorUtilities.CreateInstance`, threading the config into the provider's constructor.
The enum lives in
[`ServicesProviderType`](../src/Dsm.Shared/Options/ServicesProviderType.cs)
(`YamlFile`, `Docker`, `Swarm`, `Traefik`).

## Provider.App runtime

[`Program.cs`](../src/Dsm.Provider.App/Program.cs) builds a generic host, loads configuration from
appsettings.json, the YAML config files (see [Configuration](#configuration) below), and
`DSM_`-prefixed env vars, and registers
[`ProviderService`](../src/Dsm.Provider.App/ProviderService.cs) as a `BackgroundService`.

`ProviderService.ExecuteAsync` runs a continuous loop driven by a `PeriodicTimer` ticking every
`ProviderOptions.RefreshInterval` (default 60 s). On each tick:

1. Read [`ProviderOptions.ServicesProviders`](../src/Dsm.Shared/Options/ProviderOptions.cs) — a
   list of typed configs, each carrying the provider's own settings.
2. For each entry, resolve an `IServicesProvider` from the factory and call `ListServices()`.
   Per-provider exceptions are caught and logged; one failing provider doesn't take the others
   down or stop the loop.
3. If the aggregated service list is non-empty, POST it to the Manager API via the Refit
   [`IDcmClient`](../src/Dsm.Shared/ApiClients/IDcmClient.cs). The response is a
   `Dictionary<string, List<Service>>` keyed by manager type name (see
   [managers.md](managers.md#request-flow-write-path)); `ProviderService` logs the per-manager
   counts and entries. An empty aggregated list is a no-op — useful when the worker starts before
   any containers exist.

The Refit client's `HttpClient` is built by
[`ClientFactory`](../src/Dsm.Shared/ApiClients/ClientFactory.cs) using `ProviderOptions.ApiUrl` as
the base address.

## Configuration

[`ProviderOptions`](../src/Dsm.Shared/Options/ProviderOptions.cs) is bound, in this order of
precedence (later wins), from:

1. `appsettings.json` (loaded by `Host.CreateDefaultBuilder`).
2. `provider-config.yml` or `provider-config.yaml` next to the binary, *and* the same filenames
   under `/config/` — both locations are searched, both are optional. The `/config/` mount is the
   conventional path used by [provider.Dockerfile](../docker/provider.Dockerfile).
3. `DSM_ProviderOptions__*` environment variables.

It has two layers: process-global fields, and a typed list of per-provider
`ServicesProviderConfig` entries.

**Top-level (global, on `ProviderOptions`):**

| Key | Required | Purpose |
|---|---|---|
| `ApiUrl` | yes | Base URL of the Manager API (e.g. `http://dsm-api:5270`) |
| `RefreshInterval` | no (default 60s) | How often to poll all providers and POST the result |
| `ServicesProviders` | yes (≥1 entry) | List of `ServicesProviderConfig` entries — see below |

**Per-provider (`ServicesProviderConfig` fields):**

| Key | Applies to | Purpose |
|---|---|---|
| `ServicesProviderType` | all | Discriminator: `Docker`, `Swarm`, `YamlFile`, `Traefik` |
| `Hostname` | required for Docker, Swarm, Traefik | Value stamped onto every service's `Hostname` field — drives the `(Name, Hostname)` dedupe key the combiner uses, and the `server` field in Homepage YAML / `host=…` tag in Dashy |
| `AreServiceHostsHttps` | Docker, Swarm, Traefik | If true, generated URLs use `https://` |
| `DockerLabelPrefix` | required for Docker, Swarm | Prefix (e.g. `dsm`) used to select labels on containers — `dsm.name`, `dsm.category`, etc. See [Container label vocabulary](#container-label-vocabulary) |
| `ServicesYamlFilePath` | required for YamlFile | Path to the YAML file |
| `TraefikApiUrl` | required for Traefik | Base URL of the Traefik API (e.g. `http://traefik:8080`) |

### Validation

[`ProviderOptionsValidator`](../src/Dsm.Shared/Options/ProviderOptions.cs) runs at startup and
fails the host with a list of errors if any per-provider required field is missing. Specifically:

- `Traefik` — `TraefikApiUrl` and `Hostname` must be set.
- `YamlFile` — `ServicesYamlFilePath` must be set. (`Hostname` is optional — bare-metal services
  often want their own hostnames per entry.)
- `Docker` / `Swarm` — `DockerLabelPrefix` and `Hostname` must both be set.

A misconfigured deployment fails fast with a clear message rather than silently producing an
empty service list.

Example `provider-config.yml` (the production-typical form):

```yaml
ProviderOptions:
  ApiUrl: http://dsm-api:5270
  ServicesProviders:
    - ServicesProviderType: Docker
      Hostname: media-01
      AreServiceHostsHttps: true
      DockerLabelPrefix: dsm
    - ServicesProviderType: YamlFile
      ServicesYamlFilePath: /etc/dsm/services.yml
```

Equivalent `appsettings.json`:

```json
{
  "ProviderOptions": {
    "ApiUrl": "http://dsm-api:5270",
    "ServicesProviders": [
      { "ServicesProviderType": "Docker", "Hostname": "media-01", "AreServiceHostsHttps": true, "DockerLabelPrefix": "dsm" },
      { "ServicesProviderType": "YamlFile", "ServicesYamlFilePath": "/etc/dsm/services.yml" }
    ]
  }
}
```

Equivalent env-var form:

```sh
DSM_ProviderOptions__ApiUrl=http://dsm-api:5270 \
DSM_ProviderOptions__ServicesProviders__0__ServicesProviderType=Docker \
DSM_ProviderOptions__ServicesProviders__0__Hostname=media-01 \
DSM_ProviderOptions__ServicesProviders__0__AreServiceHostsHttps=true \
DSM_ProviderOptions__ServicesProviders__0__DockerLabelPrefix=dsm \
DSM_ProviderOptions__ServicesProviders__1__ServicesProviderType=YamlFile \
DSM_ProviderOptions__ServicesProviders__1__ServicesYamlFilePath=/etc/dsm/services.yml \
dotnet run --project src/Dsm.Provider.App
```

### Container label vocabulary

`Docker` and `Swarm` providers translate container labels into `Service` fields via
[`ContainerLabelServiceFactory`](../src/Dsm.Providers/Services/ContainerLabelServiceFactory.cs).
For prefix `dsm`, the recognized labels are:

| Label | `Service` field | Notes |
|---|---|---|
| `dsm.name` | `Name` | Defaults to the container name (Swarm: with the stack namespace stripped) |
| `dsm.url` | `Url` | If absent, recovered from a Traefik `Host(...)` router rule (see `dsm.traefik.router` below) |
| `dsm.category` | `Category` | Free-form; matched case-insensitively against `ServiceDefaultOptions.Categories` for icons |
| `dsm.icon` | `Icon` | Plain icon name, or a prefixed lookup (`hl-…`, `sh-…`) — see [service-defaults.md](service-defaults.md#icon-lookup) |
| `dsm.image_path` | `ImageUrl` | Absolute URL or path relative to `Url`; resolved by the manager-side defaults factory |
| `dsm.ignore` | `Ignore` | `"true"` to drop this container before the manager even sees it |
| `dsm.service_defaults_name` | `ServiceDefaultsName` | Alias to a different defaults entry — e.g. `dsm.service_defaults_name=plex` on a service named "Plex Beta" |
| `dsm.traefik.router` | (Url recovery) | Name of a Traefik router whose `Host(...)` rule should be used to derive the service URL when `dsm.url` is absent. The provider parses `traefik.http.routers.<router>.rule` from the same container's labels via [`TraefikRuleParser`](../src/Dsm.Providers/ServicesProviders/Traefik/TraefikRuleParser.cs) |

Labels without the configured prefix are ignored. `Url` recovery from Traefik labels is a
convenience for containers that already declare their hostname to Traefik — you don't need to
duplicate it in `dsm.url`.

### Wire model fields the provider stamps

`Service` (the wire contract) carries two fields the provider can populate that aren't already in
the table above:

- `Autogenerated` — the manager always overwrites this to `true` on POST, so providers can leave
  it at its default. Documented for completeness because it ships across the wire.
- `ServiceDefaultsName` — set explicitly via `dsm.service_defaults_name`, or via the YAML file
  provider's `service_defaults_name` key. Used by the manager-side defaults factory and the
  Homepage widget matcher; see [service-defaults.md](service-defaults.md#lookup-key) and
  [managers.md](managers.md#service-widgets).

## Folder map

```
Dsm.Providers/
├── ServicesProviders/
│   ├── IServicesProvider.cs              Extension point
│   ├── DockerServicesProvider.cs
│   ├── SwarmServicesProvider.cs
│   ├── YamlFileServicesProvider.cs
│   ├── TraefikServicesProvider.cs
│   ├── Traefik/
│   │   ├── TraefikRouter.cs              DTO for /api/http/routers
│   │   ├── ITraefikApiClient.cs          Refit interface for Traefik API
│   │   ├── TraefikApiClientFactory.cs    Builds ITraefikApiClient from options
│   │   └── TraefikRuleParser.cs          Host(...) rule → URL helper
│   ├── ServicesProviderFactory.cs        Builds providers via ActivatorUtilities
│   └── ServicesProviderUtilities.cs      Shared formatting helpers
├── Services/
│   └── ContainerLabelServiceFactory.cs   Container label dict → Service
└── Hosting/
    └── HostBuilderConfiguration.cs       DI + provider-config.yaml wiring

Dsm.Provider.App/
├── Program.cs                            Host builder, DSM_ env var prefix
└── ProviderService.cs                    BackgroundService loop
```

## Adding a new provider

1. Implement [`IServicesProvider`](../src/Dsm.Providers/ServicesProviders/IServicesProvider.cs). The
   constructor should accept a `ServicesProviderConfig` as its last argument; everything else
   comes from DI. Re-use
   [`ContainerLabelServiceFactory`](../src/Dsm.Providers/Services/ContainerLabelServiceFactory.cs)
   if your source exposes labels; otherwise build `Service` objects directly.
2. Add a variant to
   [`ServicesProviderType`](../src/Dsm.Shared/Options/ServicesProviderType.cs).
3. Add any new type-specific fields (nullable) to
   [`ServicesProviderConfig`](../src/Dsm.Shared/Options/ProviderOptions.cs).
4. Extend the switch in
   [`ServicesProviderFactory.Create`](../src/Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs)
   with an `ActivatorUtilities.CreateInstance<T>(sp, config)` line for your new provider.

## Testing

```sh
dotnet test src/Dsm.Providers.Tests
```

The `DockerServicesProvider` tests run against whatever Docker daemon your environment exposes to
`Docker.DotNet`'s default client configuration. If you don't have Docker locally, scope the run:

```sh
dotnet test src/Dsm.Providers.Tests --filter "FullyQualifiedName!~DockerServicesProvider"
```
