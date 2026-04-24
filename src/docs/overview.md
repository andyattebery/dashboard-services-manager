# dotnet/ — Overview

A .NET port of the Ruby/Sinatra app in [src/](../../src/). Two deployables work together to keep a
[Dashy](https://dashy.to/) dashboard file in sync with the services that actually exist in your
infrastructure:

- **[Dsm.Manager.Api](../Dsm.Manager.Api/)** — ASP.NET Core Web API. Owns the dashboard YAML file
  (read/write) and the defaulting + merge rules.
- **[Dsm.Provider.App](../Dsm.Provider.App/)** — console `BackgroundService`. Enumerates services
  from an environment (Docker, Swarm, or a YAML file) and POSTs them to the Manager API.

Everything else is either a library consumed by those two (`Dsm.Managers`, `Dsm.Providers`,
`Dsm.Shared`) or a test project.

## Project map

| Project | Role | Tests |
|---|---|---|
| [Dsm.Manager.Api](../Dsm.Manager.Api/) | HTTP entry point for the manager side | — |
| [Dsm.Managers](../Dsm.Managers/) | Dashboard read/write, defaulting, combiner; see [managers.md](managers.md) | [Dsm.Managers.Tests](../Dsm.Managers.Tests/) |
| [Dsm.Provider.App](../Dsm.Provider.App/) | Hosts the background worker that pushes services | — |
| [Dsm.Providers](../Dsm.Providers/) | `IServicesProvider` implementations (Docker/Swarm/YAML); see [providers.md](providers.md) | [Dsm.Providers.Tests](../Dsm.Providers.Tests/) |
| [Dsm.Shared](../Dsm.Shared/) | Wire models, Refit client, shared options | [Dsm.Shared.Tests](../Dsm.Shared.Tests/) |

Target framework is `net10.0` across the solution ([global.json](../global.json) pins SDK 10.0.100
with `latestMinor` roll-forward).

## Data flow

```
[infra: Docker / Swarm / YAML file]
        │  IServicesProvider.ListServices()
        ▼
Dsm.Provider.App  (BackgroundService)
        │  POST /dashboard-services  (List<Service>, JSON via Refit)
        ▼
Dsm.Manager.Api → DashboardCommandProcessor
        │  filter (Ignore / blank URL / IgnoredServiceNames)
        │  apply defaults (WithDefaultsServiceFactory)
        │  merge with existing (ServicesCombiner)
        ▼
IDashboardManager (only Dashy today) → dashy_conf.yml
```

The wire contract is [`Service`](../Dsm.Shared/Models/Service.cs) in `Dsm.Shared`; Refit serializes
it as JSON.

The HTTP surface is small: [DashboardController](../Dsm.Manager.Api/Controllers/DashboardController.cs)
exposes `POST /dashboard-services` (update) and `GET /dashboard-services` (list).

## Configuration at a glance

- **Manager API** loads [`service-defaults.yaml`](../service-defaults.yaml) (shipped, required) and
  then [`manager-config.yaml`](../manager-config.yaml) (user, optional; overrides the shipped
  defaults) via
  [HostBuilderConfiguration.ConfigureConfiguration](../Dsm.Managers/Hosting/HostBuilderConfiguration.cs).
  Two sections: `ManagerOptions` (dashboard type + file path + ignored-name list) and
  `ServiceDefaultOptions` (per-service defaults, categories, icon sources) — see
  [service-defaults.md](service-defaults.md).
- Both files go through [`AddNormalizedYamlFile`](../Dsm.Shared/Configuration/NormalizedYamlConfigurationSource.cs)
  which strips underscores from keys at load time, so `snake_case`, `camelCase`, and `PascalCase`
  all bind. Dashes in dict keys (e.g. `calibre-web`) are preserved.
- **Provider App** reads `DSM_`-prefixed environment variables
  ([Program.cs:25](../Dsm.Provider.App/Program.cs#L25)) and binds `ProviderOptions` from them.

## Build / Run / Test

```sh
cd dotnet

# build
dotnet build Dsm.sln

# run the API (defaults: http://localhost:5270, https://localhost:7015)
dotnet run --project Dsm.Manager.Api

# run the provider worker against a running API
DSM_ProviderOptions__ApiUrl=http://localhost:5270 \
DSM_ProviderOptions__Hostname=media-01 \
DSM_ProviderOptions__ServicesProviders__0__ServicesProviderType=Docker \
DSM_ProviderOptions__ServicesProviders__0__DockerLabelPrefix=dsm \
DSM_ProviderOptions__ServicesProviders__0__AreServiceHostsHttps=true \
dotnet run --project Dsm.Provider.App

# tests (skip walkxcode-icon tests that hit the public CDN)
dotnet test Dsm.sln --filter "TestCategory!=Network"
```

Docker images are built from [api.Dockerfile](../api.Dockerfile) and
[provider.Dockerfile](../provider.Dockerfile).

## Further reading

- [managers.md](managers.md) — how the manager side is put together, the combiner rule, and how to
  plug in a new dashboard format.
- [service-defaults.md](service-defaults.md) — the defaulting factory, `service-defaults.yaml`,
  per-service override rules, and the icon-lookup chain.
- [providers.md](providers.md) — how providers discover services and how to add a new one.
