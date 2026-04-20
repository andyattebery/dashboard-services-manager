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
  `ProviderOptions.DockerLabelPrefix`-prefixed labels to populate the
  [`Service`](../Dsm.Shared/Models/Service.cs) fields.
- [`SwarmServicesProvider`](../Dsm.Providers/ServicesProviders/SwarmServicesProvider.cs) — same
  idea against a Docker Swarm task list.
- [`YamlFileServicesProvider`](../Dsm.Providers/ServicesProviders/YamlFileServicesProvider.cs) —
  reads a flat YAML file at `ProviderOptions.ServicesYamlFilePath`. Useful for bare-metal services
  that aren't containerized.
- [`TraefikServicesProvider`](../Dsm.Providers/ServicesProviders/TraefikServicesProvider.cs) —
  calls Traefik's [`GET /api/http/routers`](https://doc.traefik.io/traefik/reference/install-configuration/api-dashboard/#opt-apihttprouters)
  and turns enabled routers with a `Host(...)` rule into services. Skips `@internal` routers and
  strips `@provider` / `-docker-compose` suffixes from the service name.

The shared translation from container labels to a `Service` lives in
[`ContainerLabelServiceFactory`](../Dsm.Providers/Services/ContainerLabelServiceFactory.cs). The
`Host(...)` rule-parsing helper it shares with the Traefik provider lives in
[`TraefikRuleParser`](../Dsm.Providers/ServicesProviders/Traefik/TraefikRuleParser.cs).

Provider selection is done by the
[`ServicesProviderFactory`](../Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs), which
accepts either a
[`ServicesProviderType`](../Dsm.Providers/ServicesProviders/ServicesProviderType.cs) enum value or a
free-form string (`"docker"`, `"swarm"`, `"yaml"` / `"yaml_file"` / `"yamlfile"`, `"traefik"`).

## Provider.App runtime

[`Program.cs`](../Dsm.Provider.App/Program.cs) builds a generic host, loads configuration from
`DSM_`-prefixed env vars, and registers
[`ProviderService`](../Dsm.Provider.App/ProviderService.cs) as a `BackgroundService`.

`ProviderService.ExecuteAsync` does, on startup:

1. Read [`ProviderOptions.ServicesProviderTypes`](../Dsm.Shared/Options/ProviderOptions.cs) (plural,
   list). If empty, fall back to the legacy singular `ProviderOptions.ServicesProviderType`.
2. For each type, resolve an `IServicesProvider` from the factory and call `ListServices()`.
3. POST the resulting services to the Manager API via the Refit
   [`IDcmClient`](../Dsm.Shared/ApiClients/IDcmClient.cs).

The Refit client's `HttpClient` is built by
[`ClientFactory`](../Dsm.Shared/ApiClients/ClientFactory.cs) using `ProviderOptions.ApiUrl` as the
base address.

## Configuration

All fields on [`ProviderOptions`](../Dsm.Shared/Options/ProviderOptions.cs), bound from
`DSM_ProviderOptions__*` env vars:

| Key | Purpose |
|---|---|
| `ApiUrl` | Base URL of the Manager API (e.g. `http://dsm-api:5270`) |
| `Hostname` | Value stamped into each service's `Hostname` field; drives the `host=...` tag used by the combiner |
| `DockerLabelPrefix` | Prefix (e.g. `dsm`) used to select labels on containers — `dsm.name`, `dsm.category`, etc. |
| `AreServiceHostsHttps` | If true, generated URLs use `https://` |
| `ServicesProviderType` | Legacy single-provider selector (`docker`, `swarm`, `yaml`) |
| `ServicesProviderTypes` | Preferred plural form: a list, so one Provider.App instance can pull from several sources in turn |
| `ServicesYamlFilePath` | Path to the YAML file when using `YamlFileServicesProvider` |
| `TraefikApiUrl` | Base URL of the Traefik API (e.g. `http://traefik:8080`) when using `TraefikServicesProvider` |

Example:

```sh
DSM_ProviderOptions__ApiUrl=http://localhost:5270 \
DSM_ProviderOptions__Hostname=media-01 \
DSM_ProviderOptions__DockerLabelPrefix=dsm \
DSM_ProviderOptions__ServicesProviderTypes__0=docker \
DSM_ProviderOptions__ServicesProviderTypes__1=yaml \
DSM_ProviderOptions__ServicesYamlFilePath=/etc/dsm/services.yml \
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
│   ├── ServicesProviderFactory.cs        Enum + string overloads
│   ├── ServicesProviderType.cs           Enum (YamlFile, Docker, Swarm, Traefik)
│   └── ServicesProviderUtilities.cs      Shared formatting helpers
├── Services/
│   └── ContainerLabelServiceFactory.cs   Container label dict → Service
└── ServiceCollectionConfiguration.cs     DI wiring

Dsm.Provider.App/
├── Program.cs                            Host builder, DSM_ env var prefix
└── ProviderService.cs                    BackgroundService loop
```

## Adding a new provider

1. Implement [`IServicesProvider`](../Dsm.Providers/ServicesProviders/IServicesProvider.cs).
   Re-use [`ContainerLabelServiceFactory`](../Dsm.Providers/Services/ContainerLabelServiceFactory.cs)
   if your source exposes labels; otherwise build `Service` objects directly.
2. Add a variant to
   [`ServicesProviderType`](../Dsm.Providers/ServicesProviders/ServicesProviderType.cs).
3. Register your new class as a transient in
   [`ServiceCollectionConfiguration`](../Dsm.Providers/ServiceCollectionConfiguration.cs).
4. Extend both overloads of
   [`ServicesProviderFactory.Create`](../Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs) —
   the enum switch and the string switch in `GetServiceProviderType`.

## Testing

```sh
dotnet test dotnet/Dsm.Providers.Tests
```

The `DockerServicesProvider` tests run against whatever Docker daemon your environment exposes to
`Docker.DotNet`'s default client configuration. If you don't have Docker locally, scope the run:

```sh
dotnet test dotnet/Dsm.Providers.Tests --filter "FullyQualifiedName!~DockerServicesProvider"
```
