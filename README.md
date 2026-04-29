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

### Running this app

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

### Local development from this repo

```sh
cd docker-compose
docker compose --env-file env up --build -d
```

Compose auto-merges [docker-compose/docker-compose.override.yaml](docker-compose/docker-compose.override.yaml),
which adds `build:` directives so the API and provider images are built locally from
[docker/api.Dockerfile](docker/api.Dockerfile) and [docker/provider.Dockerfile](docker/provider.Dockerfile)
instead of pulled from the registry. The override file is dev-only — production hosts that
copied just the four files above never see it.

For the .NET dev loop without containers:

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
