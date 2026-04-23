# Dsm.Providers + Dsm.Provider.App

Discover the services running in an environment and push them to the Manager API. The library
([Dsm.Providers](../Dsm.Providers/)) holds the provider implementations; the host
([Dsm.Provider.App](../Dsm.Provider.App/)) runs them on a schedule and does the HTTP call.

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

- [`DockerServicesProvider`](../Dsm.Providers/ServicesProviders/DockerServicesProvider.cs) — uses
  [Docker.DotNet](https://github.com/dotnet/Docker.DotNet) to list running containers and reads
  `ServicesProviderConfig.DockerLabelPrefix`-prefixed labels to populate the
  [`Service`](../Dsm.Shared/Models/Service.cs) fields.
- [`SwarmServicesProvider`](../Dsm.Providers/ServicesProviders/SwarmServicesProvider.cs) — same
  idea against a Docker Swarm task list.
- [`YamlFileServicesProvider`](../Dsm.Providers/ServicesProviders/YamlFileServicesProvider.cs) —
  reads a flat YAML file at `ServicesProviderConfig.ServicesYamlFilePath`. Useful for bare-metal
  services that aren't containerized.
- [`TraefikServicesProvider`](../Dsm.Providers/ServicesProviders/TraefikServicesProvider.cs) —
  calls Traefik's [`GET /api/http/routers`](https://doc.traefik.io/traefik/reference/install-configuration/api-dashboard/#opt-apihttprouters)
  at `ServicesProviderConfig.TraefikApiUrl` and turns enabled routers with a `Host(...)` rule into
  services. Skips `@internal` routers and strips `@provider` / `-docker-compose` suffixes from the
  service name.

The shared translation from container labels to a `Service` lives in
[`ContainerLabelServiceFactory`](../Dsm.Providers/Services/ContainerLabelServiceFactory.cs). The
`Host(...)` rule-parsing helper it shares with the Traefik provider lives in
[`TraefikRuleParser`](../Dsm.Providers/ServicesProviders/Traefik/TraefikRuleParser.cs).

Provider selection is done by the
[`ServicesProviderFactory`](../Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs), which
takes a [`ServicesProviderConfig`](../Dsm.Shared/Options/ProviderOptions.cs) entry from
`ProviderOptions.ServicesProviders` and builds the matching provider via
`ActivatorUtilities.CreateInstance`, threading the config into the provider's constructor.
The enum lives in
[`ServicesProviderType`](../Dsm.Shared/Options/ServicesProviderType.cs)
(`YamlFile`, `Docker`, `Swarm`, `Traefik`).

## Provider.App runtime

[`Program.cs`](../Dsm.Provider.App/Program.cs) builds a generic host, loads configuration from
`DSM_`-prefixed env vars, and registers
[`ProviderService`](../Dsm.Provider.App/ProviderService.cs) as a `BackgroundService`.

`ProviderService.ExecuteAsync` does, on startup:

1. Read [`ProviderOptions.ServicesProviders`](../Dsm.Shared/Options/ProviderOptions.cs) — a list of
   typed configs, each carrying the provider's own settings.
2. For each entry, resolve an `IServicesProvider` from the factory and call `ListServices()`.
3. POST the resulting services to the Manager API via the Refit
   [`IDcmClient`](../Dsm.Shared/ApiClients/IDcmClient.cs). The response is a
   `Dictionary<string, List<Service>>` keyed by manager type name (see
   [managers.md](managers.md#request-flow-write-path)); `ProviderService` logs the per-manager
   counts and entries.

The Refit client's `HttpClient` is built by
[`ClientFactory`](../Dsm.Shared/ApiClients/ClientFactory.cs) using `ProviderOptions.ApiUrl` as the
base address.

## Configuration

[`ProviderOptions`](../Dsm.Shared/Options/ProviderOptions.cs) is bound, in this order of
precedence (later wins), from:

1. `appsettings.json` (shipped with the app)
2. `provider-config.yml` or `provider-config.yaml` next to the binary (optional)
3. `DSM_ProviderOptions__*` environment variables

It has two layers: process-global fields, and a typed list of per-provider
`ServicesProviderConfig` entries.

**Top-level (global):**

| Key | Purpose |
|---|---|
| `ApiUrl` | Base URL of the Manager API (e.g. `http://dsm-api:5270`) |
| `Hostname` | Value stamped into each service's `Hostname` field; drives the `host=...` tag used by the combiner |
| `RefreshInterval` | How often to poll all providers (default 60s) |
| `ServicesProviders` | List of `ServicesProviderConfig` entries — see below |

**Per-provider (`ServicesProviderConfig` fields):**

| Key | Applies to | Purpose |
|---|---|---|
| `ServicesProviderType` | all | Discriminator: `Docker`, `Swarm`, `YamlFile`, `Traefik` |
| `AreServiceHostsHttps` | Docker, Swarm, Traefik | If true, generated URLs use `https://` |
| `DockerLabelPrefix` | Docker, Swarm | Prefix (e.g. `dsm`) used to select labels on containers — `dsm.name`, `dsm.category`, etc. |
| `ServicesYamlFilePath` | YamlFile | Path to the YAML file |
| `TraefikApiUrl` | Traefik | Base URL of the Traefik API (e.g. `http://traefik:8080`) |

Example `appsettings.json`:

```json
{
  "ProviderOptions": {
    "ApiUrl": "http://localhost:5270",
    "Hostname": "media-01",
    "ServicesProviders": [
      { "ServicesProviderType": "Docker",  "AreServiceHostsHttps": true,  "DockerLabelPrefix": "dsm" },
      { "ServicesProviderType": "YamlFile", "ServicesYamlFilePath": "/etc/dsm/services.yml" }
    ]
  }
}
```

Equivalent `provider-config.yml`:

```yaml
ProviderOptions:
  ApiUrl: http://localhost:5270
  Hostname: media-01
  ServicesProviders:
    - ServicesProviderType: Docker
      AreServiceHostsHttps: true
      DockerLabelPrefix: dsm
    - ServicesProviderType: YamlFile
      ServicesYamlFilePath: /etc/dsm/services.yml
```

Equivalent env-var form:

```sh
DSM_ProviderOptions__ApiUrl=http://localhost:5270 \
DSM_ProviderOptions__Hostname=media-01 \
DSM_ProviderOptions__ServicesProviders__0__ServicesProviderType=Docker \
DSM_ProviderOptions__ServicesProviders__0__AreServiceHostsHttps=true \
DSM_ProviderOptions__ServicesProviders__0__DockerLabelPrefix=dsm \
DSM_ProviderOptions__ServicesProviders__1__ServicesProviderType=YamlFile \
DSM_ProviderOptions__ServicesProviders__1__ServicesYamlFilePath=/etc/dsm/services.yml \
dotnet run --project Dsm.Provider.App
```

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

1. Implement [`IServicesProvider`](../Dsm.Providers/ServicesProviders/IServicesProvider.cs). The
   constructor should accept a `ServicesProviderConfig` as its last argument; everything else
   comes from DI. Re-use
   [`ContainerLabelServiceFactory`](../Dsm.Providers/Services/ContainerLabelServiceFactory.cs)
   if your source exposes labels; otherwise build `Service` objects directly.
2. Add a variant to
   [`ServicesProviderType`](../Dsm.Shared/Options/ServicesProviderType.cs).
3. Add any new type-specific fields (nullable) to
   [`ServicesProviderConfig`](../Dsm.Shared/Options/ProviderOptions.cs).
4. Extend the switch in
   [`ServicesProviderFactory.Create`](../Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs)
   with an `ActivatorUtilities.CreateInstance<T>(sp, config)` line for your new provider.

## Testing

```sh
dotnet test dotnet/Dsm.Providers.Tests
```

The `DockerServicesProvider` tests run against whatever Docker daemon your environment exposes to
`Docker.DotNet`'s default client configuration. If you don't have Docker locally, scope the run:

```sh
dotnet test dotnet/Dsm.Providers.Tests --filter "FullyQualifiedName!~DockerServicesProvider"
```
