# Dashboard Services Manager

Keep a self-hosted dashboard's config file in sync with the services actually running in your
infrastructure — without clobbering anything you typed by hand.

DSM runs as two pieces. **Providers** walk your environment (Docker, Docker Swarm, Traefik, or a
static YAML file) and POST the services they find to a **Manager API** that owns the dashboard's
YAML config. The manager merges those auto-discovered entries into the existing file, applies
per-service defaults (icons, categories, hostname-templated names, optional Homepage widgets), and
preserves every hand-written entry. Re-runs are idempotent — the file is left alone when nothing
changed, so dashboards don't reload spuriously.

## Why

Dashy and Homepage are great, but their YAML config files drift out of sync with reality every
time you spin a new container, retire an old one, or move a service to a different host.
Hand-editing the file on every change is tedious; nuking and regenerating it loses the
hand-curated entries that aren't auto-discoverable (external links, notes, links to bookmarks).
DSM splits the file into "stuff that came from infra" (auto-managed) and "stuff a human typed"
(left alone), then keeps just the first half in sync.

## At a glance

- **[Manager API](src/Dsm.Manager.Api/)** — ASP.NET Core. Owns the dashboard YAML file. Two
  endpoints: `POST /dashboard-services` (push) and `GET /dashboard-services` (pull).
- **[Provider App](src/Dsm.Provider.App/)** — `BackgroundService`. Polls one or more configured
  providers on a `RefreshInterval` (default 60 s) and POSTs the results.

Multiple Provider Apps — one per host, each with its own configured sources — can fan into a
single Manager API. The manager dedupes services per `(Name, Hostname)`.

### Supported sources and sinks

- **Sources (providers):** Docker · Docker Swarm · Traefik · YAML file
- **Sinks (dashboards):** [Dashy](https://dashy.to/) · [Homepage](https://gethomepage.dev/)

## Quick start

The production path is Docker Compose — [docker-compose.yaml](docker-compose.yaml) already wires
the manager and a single Docker-source provider together:

```sh
docker compose up -d
```

Configure the manager with [manager-config.yaml](manager-config.yaml) (dashboard type, file paths,
per-service overrides) and the provider with [provider-config.yaml](provider-config.yaml) (API
URL, hostname, which providers to enable). Both can also be driven by `DSM_`-prefixed environment
variables.

For local development:

```sh
dotnet build src/Dsm.sln
dotnet run --project src/Dsm.Manager.Api
dotnet test src/Dsm.sln --filter "TestCategory!=Network"
```

The `TestCategory!=Network` filter skips tests that hit the public jsDelivr CDN for icon resolution.

## Documentation

- **[docs/overview.md](docs/overview.md)** — architecture, data flow, configuration model,
  build / run / test
- **[docs/managers.md](docs/managers.md)** — `Dsm.Managers` internals: the combiner rule that
  preserves hand edits, dashboard-specific YAML mapping, Homepage service widgets
- **[docs/providers.md](docs/providers.md)** — `Dsm.Providers` and `Dsm.Provider.App`: how
  services are discovered, the container label vocabulary, how to add a new provider
- **[docs/service-defaults.md](docs/service-defaults.md)** — per-service defaults, icon-source
  resolution, override rules between `service-defaults.yaml` and `manager-config.yaml`
