# Configuring providers

A provider runs on each host where you want to discover services. It enumerates whatever sources
you configure (Docker, Docker Swarm, Traefik, a hand-maintained YAML file) and POSTs the result
to the manager API on every refresh tick. Multiple provider hosts can fan into the same manager
— each host's services keep their own `Hostname`, and when two sources publish the same service
the manager merges their copies into one entry (see
[Two sources, one service](#two-sources-one-service)).

Configuration lives in [`provider-config.yaml`](../docker-compose/provider-config.yaml).

## Provider config shape

```yaml
ProviderOptions:
  ApiUrl: http://dashboard-services-manager-api:8080   # where to POST results
  Hostname: docker-01                                  # optional, override for provider identity
  RefreshInterval: 00:01:00                            # optional, default 60s
  ServicesProviders:
    - ServicesProviderType: Docker
      Hostname: media-01
      AreServiceHostsHttps: true
      DockerLabelPrefix: dsm
    # ...more providers...
```

`ServicesProviders` is a list — you can run several on the same host. Each tick, the provider
calls every entry, aggregates the results, and POSTs them as a single batch. If one source
fails on a tick (e.g. Traefik unreachable), the others still post; the failing one logs an
error and is retried on the next tick. Its entries stay on the dashboard until it posts again,
or until it has been silent for the manager's `StaleProviderTimeout` (default one hour; see
[managers.md](managers.md#services-from-providers-that-stop-posting)).

## Provider identity

Each provider source is assigned a `ProviderId` that scopes how services are replaced. When a
provider POSTs, only services from that same provider are replaced — services contributed by
other providers for the same hostname are preserved.

The ProviderId is derived automatically as `{hostname}-{type}`, with an index suffix when
multiple sources share the same type. The hostname is resolved in order:

1. `ProviderOptions.Hostname` (explicit override)
2. `HOSTNAME` environment variable (set in docker-compose)
3. Machine hostname (`Environment.MachineName`)

For example, a deployment on docker-01 with three Traefik sources and one Docker source:
`docker-01-traefik-0`, `docker-01-traefik-1`, `docker-01-traefik-2`, `docker-01-docker`.

You can also override the ProviderId per source by setting `ProviderId` directly on a
`ServicesProviders` entry. The manager reads the source type from the end of the ID when it
merges copies of a service, so keep the `-docker`, `-swarm`, `-traefik` or `-yamlfile` ending
(an index may follow it). An ID without one ranks after every type.

Removing a source, changing the hostname, or changing how many sources of one type a provider has
(which renumbers the index) leaves entries under IDs that no longer post. The manager removes
those once its `StaleProviderTimeout` passes.

## Two sources, one service

Sources often overlap. On a host that runs both a Docker provider and a Traefik that another
provider reads, the same service can arrive twice: once from the container's labels, once from
the Traefik route. The manager treats copies as one service when they share a name and hostname
(after per-service defaults), or a URL and hostname, and merges them:

- **Ignore wins.** If any copy is ignored (`dsm.ignore=true`, or `ignore: true` in a YAML file),
  or its name is in `IgnoredServiceNames`, the service is hidden, even though another source
  publishes it with a URL.
- **Labels apply without a URL.** A container's `dsm.category`, `dsm.icon`, `dsm.image_path` and
  `dsm.service_defaults_name` apply even when the container has no URL of its own, as long as
  another source publishes a service with the same name on the same host, with a URL. A copy
  without a URL can only be matched by name, so a `dsm.name` that differs from the other source's
  name keeps the two apart.
- **URL:** Traefik's route first, then Docker, Swarm, and the YAML file.
- **Every other value:** Docker first, then Swarm, Traefik, and the YAML file. A Traefik copy only
  carries a router name, so container labels always apply.
- A service that no source gives a URL isn't shown.

The merged entry is owned by the source that supplied its URL, so it stays the same whichever
source posted last.

## Provider types

### Docker

```yaml
- ServicesProviderType: Docker
  Hostname: media-01
  AreServiceHostsHttps: true
  DockerLabelPrefix: dsm
```

Runs on a host with a Docker daemon (the provider container bind-mounts `/var/run/docker.sock`
in the stock compose file). Reads `<DockerLabelPrefix>.*` labels off running containers and
turns each container into a service.

A container with nothing to put in `Url` (no `<prefix>.url` label and no Traefik router rule the
provider can recover a hostname from) doesn't appear on its own. Its labels still apply when
another source publishes the same service with a URL, typically a Traefik provider reading routes
defined outside the container's labels (see
[Two sources, one service](#two-sources-one-service)). Containers running purely as backends
without a public URL stay invisible to the dashboard.

Required: `Hostname`, `DockerLabelPrefix`.

### Docker Swarm

```yaml
- ServicesProviderType: Swarm
  Hostname: swarm-01
  AreServiceHostsHttps: true
  DockerLabelPrefix: dsm
```

Same label vocabulary as the Docker provider, but enumerates Swarm services instead of running
containers. The Docker stack namespace is stripped from service names — `mystack_jellyfin`
becomes `jellyfin` before any label-based name override applies, so your dashboard entries
don't end up prefixed with the stack name.

Required: same as Docker.

### Traefik

```yaml
- ServicesProviderType: Traefik
  Hostname: edge-01
  TraefikApiUrl: http://traefik:8080
  AreServiceHostsHttps: true
```

Calls Traefik's [`/api/http/routers`](https://doc.traefik.io/traefik/operations/api/) endpoint
and turns each enabled router with a `Host(...)` rule into a service.

Filtering rules:
- Disabled routers are skipped.
- Routers whose name ends in `@internal` (Traefik's own dashboard / API routers) are skipped.
- Routers whose rule doesn't include a `Host(...)` clause are skipped — the provider needs a
  hostname to construct the service URL, and `PathPrefix`-only routers don't give it one.
- The `@<provider>` suffix (`my-router@docker`) and a trailing `-docker-compose` suffix
  (Traefik's auto-generated compose names) are stripped from the service name.

Required: `TraefikApiUrl`, `Hostname`.

### YAML file

```yaml
- ServicesProviderType: YamlFile
  ServicesYamlFilePath: /etc/dsm/services.yml
```

A hand-maintained YAML file you POST through. Useful for bare-metal services, external SaaS
links, or anything else that isn't containerized. The file is a flat list of services:

```yaml
- name: Backblaze
  url: https://backblaze.com
  category: storage
  icon: hl-backblaze

- name: PiKVM HID
  url: https://pikvm.example
  hostname: rack-01
  service_defaults_name: pikvm
```

Field names are snake_case in this file; they map 1:1 onto `Service` fields. If the file is
missing, the provider logs a warning and posts nothing rather than crashing — useful for
bootstrapping a new host where the YAML file isn't there yet.

Required: `ServicesYamlFilePath`. `Hostname` on the provider entry is optional for this type
since each YAML entry can carry its own.

## Container label vocabulary

The Docker and Swarm providers translate container labels into services. For a label prefix of
`dsm`, the recognized labels are:

| Label | Maps to | Notes |
|---|---|---|
| `dsm.name` | service name | Defaults to the container name (Swarm: with the stack namespace stripped) |
| `dsm.url` | URL | If absent, recovered from a Traefik router rule via `dsm.traefik.router` below, or taken from another source that publishes the same service. When a Traefik route is also published, the route wins. |
| `dsm.category` | category | Free-form; used by the manager to group entries on the dashboard and look up a category icon |
| `dsm.icon` | icon | Plain icon name or a CDN-prefix lookup (`hl-`, `sh-`, `mdi-`) — see [icons.md](icons.md). Most services don't need this set; DSM picks an icon automatically from the service name. |
| `dsm.image_path` | image URL | Absolute URL, or a path resolved against the service's URL (which may come from another source, such as Traefik) — see [icons.md](icons.md#image-urls) |
| `dsm.ignore` | ignore flag | `"true"` hides the service, including any copy another source such as Traefik publishes (see [Two sources, one service](#two-sources-one-service)) |
| `dsm.service_defaults_name` | defaults alias | Picks up defaults for a different service name — `dsm.service_defaults_name=plex` on a container called "Plex Beta" gives it the shipped `plex` defaults |
| `dsm.traefik.router` | (URL recovery) | Name of a Traefik router whose `Host(...)` rule should be used to build the URL when `dsm.url` is absent. Useful when Traefik already declares the hostname and you don't want to duplicate it |

Labels without your configured prefix are ignored entirely. A typical `compose.yaml` snippet:

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    labels:
      - dsm.name=Jellyfin
      - dsm.category=media
      - dsm.url=https://jellyfin.example
      - dsm.icon=hl-jellyfin
```

## Hostname stamping

`Hostname` on each provider entry is stamped onto every service that provider posts. The
manager surfaces it as Dashy's `host=<hostname>` tag and Homepage's `server:` field. It's also
the key Homepage widgets match on when you scope a widget with `server:` (see the
[managers guide](managers.md#homepage-service-widgets)).

For the YAML file provider, `Hostname` on the provider entry is the default; individual YAML
entries can carry their own `hostname:` to override.

## Refresh interval

The provider polls all configured sources every `RefreshInterval` (default 60 seconds) and
POSTs the aggregate. Cut it down (e.g. `00:00:15`) for snappier updates during active changes;
extend it for stable production hosts where you don't need second-by-second freshness.

If the merged result is identical to the last POST and your dashboard YAML didn't drift in
between, the manager skips the file write entirely — so a fast refresh interval doesn't churn
mtimes on the dashboard config files.

## Validation

Every required field is checked at startup. A misconfigured provider fails fast with a clear
error message ("ServicesProviders[0] (Docker): DockerLabelPrefix is required.") instead of
silently producing an empty service list and confusing you for an hour.

## More

- [managers.md](managers.md) — configure the manager API that receives these POSTs and writes
  the dashboard YAML.
- [configuration.md](configuration.md) — `provider-config.yaml` also accepts any standard .NET
  setting (log levels, sinks, etc.) alongside the provider-specific options above.
- [loki.md](loki.md) — forward logs to a Grafana Loki stack.
- [development/providers.md](development/providers.md) — implementation details: how the
  factory wires up new provider types, how to add one.
