# Dsm.Providers and Dsm.Provider.App

The provider side finds the services running in an environment and pushes them to the
manager. The provider implementations live in the library
[Dsm.Providers](../../src/Dsm.Providers/). The host
[Dsm.Provider.App](../../src/Dsm.Provider.App/) runs them on a timer and makes the HTTP call.
[overview.md](overview.md) shows the whole system, and [managers.md](managers.md) covers what
happens after the POST.

## Providers

A provider is one source of services:

```csharp
public interface IServicesProvider
{
    Task<List<Service>> ListServices();
}
```

[`ServicesProviderFactory.Create(ServicesProviderConfig)`](../../src/Dsm.Providers/ServicesProviders/ServicesProviderFactory.cs)
builds the provider that matches the entry's
[`ServicesProviderType`](../../src/Dsm.Providers/Options/ServicesProviderType.cs) (`Docker`,
`Swarm`, `Traefik`, `YamlFile`). It uses `ActivatorUtilities.CreateInstance` and passes the
config entry as a constructor argument. Every provider stamps the entry's `ProviderId` on each
service it returns. The Docker, Swarm and Traefik providers also stamp the entry's
`Hostname`.

Providers don't apply the manager's filters. A service without a URL, or one labelled to be
ignored, still goes over the wire. The manager stores it and applies it when merging: an ignored
copy hides the service, and a URL-less copy's labels apply to another source's copy
([managers.md](managers.md#merging-sources)).

### Docker

[`DockerServicesProvider`](../../src/Dsm.Providers/ServicesProviders/DockerServicesProvider.cs)
lists every container through [Docker.DotNet](https://github.com/dotnet/Docker.DotNet),
including stopped ones (`All = true`). The `IDockerClient` is built from Docker.DotNet's
default configuration. A container's name is its first name without the leading `/` (or its
ID), [formatted](#name-formatting). Its labels go through the
[container label](#container-labels) translation.

### Docker Swarm

[`SwarmServicesProvider`](../../src/Dsm.Providers/ServicesProviders/SwarmServicesProvider.cs)
lists Swarm services instead of containers and reads labels from each service's spec. A
service's name has its stack prefix removed: `mystack_jellyfin` becomes `jellyfin`, using the
`com.docker.stack.namespace` label. The name is then formatted the same way as for Docker.

### Traefik

[`TraefikServicesProvider`](../../src/Dsm.Providers/ServicesProviders/TraefikServicesProvider.cs)
calls `GET <TraefikApiUrl>/api/http/routers` through the Refit client
[`ITraefikApiClient`](../../src/Dsm.Providers/ServicesProviders/Traefik/ITraefikApiClient.cs).
[`TraefikApiClientFactory`](../../src/Dsm.Providers/ServicesProviders/Traefik/TraefikApiClientFactory.cs)
builds one per Traefik URL, on the named client `traefik` with a 30-second timeout.

- It keeps routers whose status is `enabled` and whose name doesn't end in `@internal`.
- A router's rule needs a `Host(...)` clause; the URL is built from its first host (see
  [Traefik rules](#traefik-rules)).
- The service name is the router name before the `@` (`jellyfin@docker` → `jellyfin`), minus
  a trailing `-docker-compose`, then [formatted](#name-formatting). A router whose service is
  `api@internal`, Traefik's own dashboard, is named `traefik`.
- No category or icon is set; the manager's defaults supply them.

### YAML file

[`YamlFileServicesProvider`](../../src/Dsm.Providers/ServicesProviders/YamlFileServicesProvider.cs)
reads `ServicesYamlFilePath`: a list of services with snake_case keys
(`UnderscoredNamingConvention`). It suits services that aren't containerized.

```yaml
- name: Router
  url: https://192.168.1.1
  category: network
  hostname: gateway
- name: Printer
  url: http://printer.lan
  service_defaults_name: octoprint
```

The keys are the `Service` fields: `name`, `url`, `category`, `icon`, `image_url`,
`hostname`, `ignore`, `service_defaults_name`. Each entry carries its own `hostname`; the
config entry's `Hostname` isn't used. Names are used exactly as written. A missing file is
logged as a warning and returns no services, so a file that hasn't been created yet doesn't
fail the cycle.

### Name formatting

Raw names go through
[`ServicesProviderUtilities.GetFormattedServiceName`](../../src/Dsm.Providers/ServicesProviders/ServicesProviderUtilities.cs).
For Docker and Swarm that happens in `ContainerLabelServiceFactory`, after it has matched
Traefik routers against the raw name. For Traefik it happens in the provider.
It turns `_` and `-` into spaces, title-cases the result, and writes the word `db` as `DB`:
`calibre-web` → `Calibre Web`, `immich_db` → `Immich DB`.

## Container labels

The Docker and Swarm providers turn labels into a `Service` with
[`ContainerLabelServiceFactory`](../../src/Dsm.Providers/Services/ContainerLabelServiceFactory.cs).
These are the labels it reads, for `DockerLabelPrefix: dsm`:

| Label | `Service` field | Notes |
|---|---|---|
| `dsm.name` | `Name` | Default: the formatted container or service name. |
| `dsm.url` | `Url` | Default: derived from a Traefik router rule ([below](#url-from-traefik-labels)). |
| `dsm.category` | `Category` | |
| `dsm.icon` | `Icon` | A plain name or a prefixed one (`hl-`, `sh-`, `mdi-`); see [icons.md](icons.md). |
| `dsm.image_path` | `ImageUrl` | Absolute, or relative to the URL; the manager resolves it ([service-defaults.md](service-defaults.md#image-url-resolution)). |
| `dsm.ignore` | `Ignore` | `true` or `false`, case-insensitive. Any other value is logged and ignored. |
| `dsm.service_defaults_name` | `ServiceDefaultsName` | Use a different defaults entry, e.g. `plex` for "Plex Beta". |
| `dsm.traefik.router` | — | Which router's rule supplies the URL. |

Labels with a different prefix are ignored, with one exception: Traefik's own
`traefik.http.routers.<router>.rule` labels.

### URL from Traefik labels

If there's no `dsm.url`, the URL comes from the `traefik.http.routers.<router>.rule` labels on
the same container or Swarm service. The router is chosen like this:

1. No router rules: no URL, and the manager drops the service.
2. `dsm.traefik.router` names one of the routers: that router's rule. A name that doesn't
   match is logged as a warning, and selection continues below.
3. Exactly one router: its rule.
4. Several routers: the one whose name equals the container name, otherwise the first router
   name in ordinal order. A warning suggests setting `dsm.traefik.router`.

Step 4 compares against the raw name, before [formatting](#name-formatting). That's the
container name without its leading `/` for Docker, and the service name without its stack
prefix for Swarm. So the container `adguard-home` picks the router `adguard-home` over
`adguard-dns`. The comparison is exact and case-sensitive, like Traefik's own router names.
`ContainerLabelServiceFactory` gets the raw name and does the formatting itself. The choice
depends only on which labels exist, never on their order, so a container's URL stays the same
from one poll to the next.

### Traefik rules

[`TraefikRuleParser`](../../src/Dsm.Providers/ServicesProviders/Traefik/TraefikRuleParser.cs)
takes the first host in the first `Host(...)` clause of a rule, ignoring backticks. In
``Host(`a.example`, `b.example`)`` it takes `a.example`. The URL is `https://<host>` when the
config entry has `AreServiceHostsHttps: true`, otherwise `http://<host>`. Rules without a
`Host(...)` clause, such as a `PathPrefix(...)`-only rule, give no URL.

## The worker loop

[`Program.cs`](../../src/Dsm.Provider.App/Program.cs) builds the host with
`Host.CreateApplicationBuilder` and sets up Serilog. It registers the provider services and
[`ProviderService`](../../src/Dsm.Provider.App/ProviderService.cs), a `BackgroundService`.
Configuration comes from `appsettings.json`, then `provider-config.{yml,yaml}` (a relative
path, then under `/config/`), then `DSM_` environment variables
([overview.md](overview.md#configuration)).

`ProviderService.ExecuteAsync`:

1. Gives each `ServicesProviderConfig` without a `ProviderId` one of the form
   `{hostname}-{type}`, lower-cased. When several entries of a type need one, it appends
   `-{index}`. The hostname is `ProviderOptions.Hostname`, else the `HOSTNAME` environment
   variable, else the machine name. The manager's `ServiceMerger` reads the type back out of
   this format, so changing it changes which source's values win
   ([managers.md](managers.md#merging-sources)).
2. Runs one cycle immediately, then one every `RefreshInterval` (a `PeriodicTimer`). A cycle:
   - Builds each provider through the factory and calls `ListServices()`. An exception from
     one provider is logged, and the remaining providers still run.
   - POSTs the combined list through the Refit client
     [`IDcmClient`](../../src/Dsm.Providers/ApiClients/IDcmClient.cs). An empty list isn't
     POSTed. HTTP errors (`ApiException`), and transport errors and timeouts (Refit's
     `ApiRequestException`, wrapping the original), are logged, and the loop carries on.
   - Touches `/tmp/dsm-heartbeat`. The provider image's `HEALTHCHECK` and the compose
     healthcheck treat the container as healthy while that file is less than two minutes
     old, so the path is fixed.

The `IDcmClient` registration
([ClientFactory.cs](../../src/Dsm.Providers/ApiClients/ClientFactory.cs)) sets
`ProviderOptions.ApiUrl` as the base address and a 30-second timeout. When `ApiKey` is set,
it also adds the `X-Api-Key` header.

## Configuration

[`ProviderOptions`](../../src/Dsm.Providers/Options/ProviderOptions.cs):

| Key | Required | Purpose |
|---|---|---|
| `ApiUrl` | yes | Base URL of the Manager API. |
| `RefreshInterval` | no (default `00:01:00`) | Time between cycles. |
| `ServicesProviders` | yes, at least one | The sources to poll (below). |
| `ApiKey` | no | Sent as `X-Api-Key`; must match the manager's `ManagerOptions.ApiKey`. |
| `Hostname` | no | Hostname part of the derived ProviderIds. |

Each `ServicesProviders` entry is a
[`ServicesProviderConfig`](../../src/Dsm.Providers/Options/ServicesProviderConfig.cs):

| Key | Applies to | Purpose |
|---|---|---|
| `ServicesProviderType` | all | `Docker`, `Swarm`, `Traefik` or `YamlFile`. |
| `Hostname` | Docker, Swarm, Traefik (required) | Stamped on every service. It becomes Homepage's `server` / Dashy's `host=` tag, and it's part of the manager's dedupe key. |
| `AreServiceHostsHttps` | Docker, Swarm, Traefik | Use `https://` for URLs built from Traefik rules. |
| `DockerLabelPrefix` | Docker, Swarm (required) | The label prefix, e.g. `dsm`. |
| `TraefikApiUrl` | Traefik (required) | Base URL of the Traefik API, e.g. `http://traefik:8080`. |
| `ServicesYamlFilePath` | YamlFile (required) | Path to the services file. |
| `ProviderId` | all | Overrides the derived ProviderId. |

The options are validated at startup, and a failure stops the host. Data annotations require
`ApiUrl` and at least one entry.
[`ProviderOptionsValidator`](../../src/Dsm.Providers/Options/ProviderOptionsValidator.cs)
checks the fields each type requires, as marked in the table.

The same configuration in each of its three forms:

```yaml
# provider-config.yaml
ProviderOptions:
  ApiUrl: http://dashboard-services-manager-api:8080
  ServicesProviders:
    - ServicesProviderType: Docker
      Hostname: media-01
      AreServiceHostsHttps: true
      DockerLabelPrefix: dsm
    - ServicesProviderType: YamlFile
      ServicesYamlFilePath: /etc/dsm/services.yml
```

```json
{
  "ProviderOptions": {
    "ApiUrl": "http://dashboard-services-manager-api:8080",
    "ServicesProviders": [
      { "ServicesProviderType": "Docker", "Hostname": "media-01", "AreServiceHostsHttps": true, "DockerLabelPrefix": "dsm" },
      { "ServicesProviderType": "YamlFile", "ServicesYamlFilePath": "/etc/dsm/services.yml" }
    ]
  }
}
```

```sh
DSM_ProviderOptions__ApiUrl=http://dashboard-services-manager-api:8080 \
DSM_ProviderOptions__ServicesProviders__0__ServicesProviderType=Docker \
DSM_ProviderOptions__ServicesProviders__0__Hostname=media-01 \
DSM_ProviderOptions__ServicesProviders__0__AreServiceHostsHttps=true \
DSM_ProviderOptions__ServicesProviders__0__DockerLabelPrefix=dsm \
DSM_ProviderOptions__ServicesProviders__1__ServicesProviderType=YamlFile \
DSM_ProviderOptions__ServicesProviders__1__ServicesYamlFilePath=/etc/dsm/services.yml \
dotnet run --project Dsm.Provider.App      # from src/
```

## Adding a provider

1. Implement `IServicesProvider`. Take a `ServicesProviderConfig` in the constructor, along
   with any DI services you need. Stamp `providerId: _config.ProviderId` on every `Service`,
   because the manager scopes replacement by it. If the source has labels, reuse
   `ContainerLabelServiceFactory`.
2. Add a value to `ServicesProviderType`.
3. Add any type-specific fields to `ServicesProviderConfig`, as nullable properties.
4. Add a case to the switch in `ServicesProviderFactory.Create`:
   `ActivatorUtilities.CreateInstance<YourProvider>(_serviceProvider, config)`.
5. Add a case to `ProviderOptionsValidator` for the fields the type requires.
6. Extend the NixOS module, [nix/module.nix](../../nix/module.nix). It writes
   `provider-config.yaml` from its own options, so add the type to the provider `type` enum
   and the new fields to `providerToAttrs`.
7. Document the provider for users in [docs/providers.md](../providers.md).

## Tests

The provider suite needs neither Docker nor Traefik. `DockerServicesProviderTests` and
`TraefikServicesProviderTests` replace `IDockerClient` and `ITraefikApiClient` with Moq mocks
fed from `TestData/`. `SwarmServicesProviderTests` covers the stack-prefix rule and router
matching on the stripped name. The label rules, including router selection, are in
`ContainerLabelServiceFactoryTests`, and rule parsing is in `TraefikRuleParserTests`.
`RefitClientTests` builds the real `IDcmClient` and Traefik clients against a stub
`HttpMessageHandler`, so a Refit upgrade that breaks client creation fails there. How to run them: [testing.md](testing.md#running-tests).

## Folder map

```
Dsm.Providers/
├── ApiClients/          IDcmClient (Refit client for the Manager API) and its registration
├── HostBuilder/         DI registration and provider configuration files
├── Options/             ProviderOptions, ServicesProviderConfig, ServicesProviderType, ProviderOptionsValidator
├── Services/            ContainerLabelServiceFactory
└── ServicesProviders/   IServicesProvider, ServicesProviderFactory, the four providers, ServicesProviderUtilities
    └── Traefik/         ITraefikApiClient, ITraefikApiClientFactory, TraefikApiClientFactory,
                         TraefikRouter, TraefikRuleParser
Dsm.Provider.App/
├── Program.cs           host setup
└── ProviderService.cs   the polling loop
```
