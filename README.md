# Dashboard Services Manager

Keep a self-hosted dashboard's config file in sync with the services actually running in your
infrastructure — without clobbering anything you typed by hand.

DSM runs as two pieces. **Providers** walk your environment (Docker, Docker Swarm, Traefik, or a
static YAML file) and POST the services they find to a **Manager API** that owns the dashboard's
YAML config. The manager merges those auto-discovered entries into the existing file, applies
per-service defaults (categories, hostname-templated names, optional Homepage widgets), and
**picks an icon automatically based on the service name** — a service called "Jellyfin" gets
the Jellyfin icon for free, no config needed. Hand-written entries are preserved across
re-runs, and writes are idempotent — the file is left alone when nothing changed, so
dashboards don't reload spuriously.

## Why

Dashboards are great, but their YAML config files drift out of sync with reality every
time you spin a new container, retire an old one, or move a service to a different host.
Hand-editing the file on every change is tedious; nuking and regenerating it loses the
hand-curated entries that aren't auto-discoverable (external links, notes, links to bookmarks).
DSM splits the file into "stuff that came from infra" (auto-managed) and "stuff a human typed"
(left alone), then keeps just the first half in sync.

## Use of AI

The architecture and initial manager and provider implementations (Dashy, Docker, Docker Swarm YAML file) were completely human-written.

AI has been used to:

- Add new manager and provider implementations
- Add new features
- Add documentation

_All AI generated code/documentation has been human-reviewed and when needed, iterated on and modified._

## At a glance

- **Manager API** — receives services from providers and updates one or more dashboard YAML
  files. Two endpoints: `POST /dashboard-services` (push) and `GET /dashboard-services` (pull).
- **Provider app** — polls one or more configured providers on a `RefreshInterval` (default
  60 s) and sends the discovered services to the manager API.

Multiple provider apps — one per host, each with its own configured sources — can fan into a
single manager API. The manager dedupes services per `(Name, Hostname)`.

### Supported Dashboards and Providers

- **Dashboards:** 
  - [Dashy](https://dashy.to/)
  - [Homepage](https://gethomepage.dev/)
- **Providers:** 
  - Docker
  - Docker Swarm
  - Traefik
  - YAML file

## Quick start

Copy the four files in [docker-compose/](docker-compose/) — `docker-compose.yaml`, `env`,
`manager-config.yaml`, and `provider-config.yaml` — to a host with Docker, then:

```sh
docker compose --env-file env pull
docker compose --env-file env up -d
```

That pulls the published images from `ghcr.io/andyattebery/dashboard-services-manager` and starts
the manager API on port 8080 plus a single Docker-source provider. Edit `manager-config.yaml`
(dashboard type, file paths, per-service overrides) and `provider-config.yaml` (API URL,
hostname, which providers to enable) for your environment. Both can also be driven by
`DSM_`-prefixed environment variables.

Both containers run as a non-root `dsm` user whose UID/GID are set at runtime from `PUID` and
`PGID` (default `1000:1000`); the [docker-compose/env](docker-compose/env) file supplies them
along with the `HOSTNAME` stamped onto every discovered service. The provider container
bind-mounts `/var/run/docker.sock`, so it also reads `DOCKER_GID` — set this to the GID that
owns `/var/run/docker.sock` on the host (commonly `998` on Linux, `0` under Docker Desktop).
The entrypoint adds the `dsm` user to a group with that GID so it can read the socket.

## Configuration

DSM reads two YAML files: manager-config.yaml for the manager API and provider-config.yaml for the rovider. The docker images look for them in `/config/`. Either file can be partially or fully replaced by `DSM_`-prefixed
environment variables; this is useful for doing all of the config in a docker compose file.

### YAML key casing

DSM strips underscores from YAML keys at load time, so `snake_case`, `camelCase`, and `PascalCase`
all bind to the same setting. Pick whichever style your config-management tooling produces — for
example `services_providers`, `servicesProviders`, and `ServicesProviders` are equivalent. Dashes
in dictionary keys (e.g. `calibre-web` under `ServiceDefaultOptions.Services`) are preserved as-is.

### `manager-config.yaml` — what the manager does

Tells the manager which dashboards to keep in sync, where each dashboard's config file lives, and
which service names to drop on the floor.

```yaml
ManagerOptions:
  DashboardManagers:
    - DashboardManagerType: Dashy             # or Homepage
      DashboardConfigDirectoryPath: /dashy_config       # the manager reads/writes <dir>/conf.yml
    - DashboardManagerType: Homepage
      DashboardConfigDirectoryPath: /homepage_config    # the manager reads/writes <dir>/services.yaml
      EnableStatusMonitoring: true                      # emits a status ping on each entry
      SourceHomepageServiceWidgetsFilePath: /homepage_config/widgets.yaml   # optional widget source
  IgnoredServiceNames:                                  # drop these names before merging
    - Dashy
    - Dashboard Services Manager

# Optional — override or extend the per-service defaults (icons, categories, name templating).
# Full reference: docs/service-defaults.md
ServiceDefaultOptions:
  Services:
    plex:
      Icon: my-custom-plex-icon
```

### `provider-config.yaml` — what each provider host does

Tells the provider where to POST results and which sources to enumerate. Run one provider per
host; they all fan into the same manager.

```yaml
ProviderOptions:
  ApiUrl: http://dashboard-services-manager-api:8080
  RefreshInterval: 00:01:00          # optional, default 60s
  ServicesProviders:
    - ServicesProviderType: Docker
      Hostname: media-01             # stamped onto every discovered service from this provider
      AreServiceHostsHttps: true
      DockerLabelPrefix: dsm         # selects container labels: dsm.name, dsm.url, dsm.icon, ...
    - ServicesProviderType: YamlFile
      ServicesYamlFilePath: /etc/dsm/services.yml
```

Required fields are validated at startup — a misconfigured provider fails fast with a clear
message instead of silently producing an empty service list.

### Overriding via environment variables

Every YAML key maps to an env var: prefix `DSM_`, join nested keys with `__` (double underscore),
index lists with `__0`, `__1`, etc. Whatever you set this way wins over the YAML file.

| YAML key | Environment variable |
|---|---|
| `ProviderOptions.ApiUrl` | `DSM_ProviderOptions__ApiUrl` |
| `ProviderOptions.RefreshInterval` | `DSM_ProviderOptions__RefreshInterval` |
| `ProviderOptions.ServicesProviders[0].ServicesProviderType` | `DSM_ProviderOptions__ServicesProviders__0__ServicesProviderType` |
| `ProviderOptions.ServicesProviders[0].Hostname` | `DSM_ProviderOptions__ServicesProviders__0__Hostname` |
| `ProviderOptions.ServicesProviders[0].DockerLabelPrefix` | `DSM_ProviderOptions__ServicesProviders__0__DockerLabelPrefix` |
| `ManagerOptions.DashboardManagers[0].DashboardManagerType` | `DSM_ManagerOptions__DashboardManagers__0__DashboardManagerType` |
| `ManagerOptions.DashboardManagers[0].DashboardConfigDirectoryPath` | `DSM_ManagerOptions__DashboardManagers__0__DashboardConfigDirectoryPath` |
| `ManagerOptions.IgnoredServiceNames[0]` | `DSM_ManagerOptions__IgnoredServiceNames__0` |

## Documentation

User guide:

- **[docs/managers.md](docs/managers.md)** — configure Dashy and Homepage outputs, status
  monitoring, Homepage widgets, per-service defaults
- **[docs/providers.md](docs/providers.md)** — configure Docker, Docker Swarm, Traefik, and
  YAML-file providers; container label vocabulary
- **[docs/icons.md](docs/icons.md)** — automatic icon resolution by service name, CDN
  prefixes (`hl-`, `sh-`, `mdi-`), image URLs, category icons

Development:

- **[docs/development/overview.md](docs/development/overview.md)** — architecture, data flow
- **[docs/development/managers.md](docs/development/managers.md)** — `Dsm.Managers` internals,
  the combiner rule, how to add a new dashboard backend
- **[docs/development/providers.md](docs/development/providers.md)** — `Dsm.Providers`
  internals, how to add a new provider
- **[docs/development/service-defaults.md](docs/development/service-defaults.md)** — defaulting
  factory, per-service overrides
- **[docs/development/icons.md](docs/development/icons.md)** — `IconResolver`, native
  pass-through vs CDN probing, fallback chain, how to add a new icon source

## Development

To build and run the images locally from a working tree (so you can iterate on code without
publishing):

```sh
cd docker-compose
docker compose --env-file env up --build -d
```

Compose auto-merges [docker-compose/docker-compose.override.yaml](docker-compose/docker-compose.override.yaml),
which adds `build:` directives so the API and provider images are built locally from
[docker/api.Dockerfile](docker/api.Dockerfile) and
[docker/provider.Dockerfile](docker/provider.Dockerfile) instead of pulled from the registry.
The override file is dev-only — production hosts that copied just the four files from the
[Quick start](#quick-start) never see it.

For the .NET dev loop without containers:

```sh
dotnet build src/Dsm.sln
dotnet run --project src/Dsm.Manager.Api
dotnet test --solution src/Dsm.sln --filter "TestCategory!=Network"
```

The `TestCategory!=Network` filter skips tests that hit the public jsDelivr CDN for icon
resolution.
