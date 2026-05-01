# Usage examples

The shipped [docker-compose/](../docker-compose/) example is a single-host smoke test —
enough to prove the pipeline works on a fresh box, but it doesn't show what a real homelab
looks like. This doc points at a public, real-world deployment as a worked reference: the
maintainer's own [andyattebery/homelab-infrastructure](https://github.com/andyattebery/homelab-infrastructure)
repo, where DSM runs alongside Dashy + Homepage on one host and provider-only stacks on
several others.

That deployment is driven by Ansible, and several files there are Jinja2 templates —
`{{ domain_name }}`, `{{ tailscale_tailnet }}`, secrets like
`{{ network_01_adguardhome_password }}` — that Ansible renders at deploy time against an
inventory and vault. **Treat the linked files as a shape reference, not a drop-in copy:**
the YAML structure, env-var names, and field choices are what's reusable; the templated
values are specific to that homelab.

## Combined dashboard stack — `docker-compose-dashboards.yml`

[`ansible/files/docker-01/docker-compose-dashboards.yml`](https://github.com/andyattebery/homelab-infrastructure/blob/main/ansible/files/docker-01/docker-compose-dashboards.yml)

Runs the manager API, the provider, **and** both dashboards (Dashy + Homepage) in one
compose stack on the dashboard host. Key shapes worth copying:

- The manager (`api-latest`) bind-mounts the Dashy and Homepage config directories
  directly: `${DOCKER_DATA_DIRECTORY}/dashy:/dashy_config` and
  `${DOCKER_DATA_DIRECTORY}/homepage/config:/homepage_config`. That's how it writes
  `conf.yml` / `services.yaml` straight into the live dashboard config — no copying.
- The manager and provider share a `${DOCKER_DATA_DIRECTORY}/dashboard-services-manager`
  directory, mounted at `/config` in both. The manager reads `manager-config.yaml`; the
  provider reads `provider-config.yaml` (rendered from a Jinja template — see below).
- Both DSM containers, plus Dashy and Homepage, get `traefik.enable=true` for the homelab's
  Traefik reverse proxy.

```yaml
dashboard-services-manager:
  image: ghcr.io/andyattebery/dashboard-services-manager:api-latest
  volumes:
    - ${DOCKER_DATA_DIRECTORY}/dashy:/dashy_config
    - ${DOCKER_DATA_DIRECTORY}/homepage/config:/homepage_config
    - ${DOCKER_DATA_DIRECTORY}/dashboard-services-manager:/config
```

## Provider-only stack — `docker-compose-dsm-provider.yaml`

[`ansible/roles/docker_compose_dashboard_services_manager_provider/files/docker-compose-dsm-provider.yaml`](https://github.com/andyattebery/homelab-infrastructure/blob/main/ansible/roles/docker_compose_dashboard_services_manager_provider/files/docker-compose-dsm-provider.yaml)

This is the compose file an Ansible role drops onto **every other Docker host** in the
homelab. Each host runs its own provider container, enumerates its own local Docker
daemon, and POSTs into the single manager API on the dashboard host — exactly the
"multiple provider apps fan into one manager" pattern from the [README](../README.md).

Worth noting: this stack uses **no `provider-config.yaml` file at all**. Everything is
configured via `DSM_`-prefixed env vars:

```yaml
environment:
  - DSM_PROVIDEROPTIONS__APIURL=https://dashboard-services-manager.${DOMAIN_NAME}
  - DSM_PROVIDEROPTIONS__SERVICESPROVIDERS__0__SERVICESPROVIDERTYPE=Docker
  - DSM_PROVIDEROPTIONS__SERVICESPROVIDERS__0__DOCKERLABELPREFIX=dsm
  - DSM_PROVIDEROPTIONS__SERVICESPROVIDERS__0__ARESERVICEHOSTSHTTPS=true
  - DSM_PROVIDEROPTIONS__SERVICESPROVIDERS__0__HOSTNAME=${HOSTNAME}
```

That maps onto the [env-var override table in the README](../README.md#overriding-via-environment-variables)
— useful when you want the role-deployed compose file to be identical on every host and
the variation comes entirely from the host's environment.

## DSM config files — `dashboard-services-manager/`

[`ansible/files/docker-01/dashboard-services-manager/`](https://github.com/andyattebery/homelab-infrastructure/tree/main/ansible/files/docker-01/dashboard-services-manager)

The four files Ansible drops into the dashboard host's
`${DOCKER_DATA_DIRECTORY}/dashboard-services-manager` directory.

### `manager-config.yaml` (static, not templated)

Two dashboard targets, status monitoring on Homepage, and a widgets file:

```yaml
ManagerOptions:
  DashboardManagers:
    - DashboardManagerType: Dashy
      DashboardConfigDirectoryPath: /dashy_config
    - DashboardManagerType: Homepage
      DashboardConfigDirectoryPath: /homepage_config
      EnableStatusMonitoring: true
      SourceHomepageServiceWidgetsFilePath: /config/homepage-service-widgets.yaml
  IgnoredServiceNames:
    - Dashy
    - Dashboard Services Manager
    - Homepage
    - Minio S3
    - Netbootxyz Assets
    - Ollama
```

The `IgnoredServiceNames` list shows a typical use: drop infra services (Minio, Ollama,
Netbootxyz) that show up via Docker labels but shouldn't surface on the dashboard. Full
option reference in [managers.md](managers.md).

### `provider-config.yaml.j2` (Jinja template)

A single provider host running **four sources at once** — one `YamlFile` for non-Docker
services, plus three `Traefik` sources, each pointed at a different host's Traefik:

```yaml
ProviderOptions:
  ApiUrl: https://dashboard-services-manager.{{ domain_name }}
  ServicesProviders:
    - ServicesProviderType: YamlFile
      ServicesYamlFilePath: /config/provider-services.yaml
      AreServiceHostsHttps: true
    - ServicesProviderType: Traefik
      Hostname: docker-01
      TraefikApiUrl: https://traefik.docker-01.{{ domain_name }}
      AreServiceHostsHttps: true
    - ServicesProviderType: Traefik
      Hostname: media-01
      TraefikApiUrl: https://traefik.media-01.{{ domain_name }}
      AreServiceHostsHttps: true
    - ServicesProviderType: Traefik
      Hostname: nas-01
      TraefikApiUrl: https://traefik.nas-01.{{ domain_name }}
      AreServiceHostsHttps: true
```

Each `Hostname:` is the value stamped onto every service that source posts (see the
[Traefik provider section](providers.md#traefik) of the providers guide). This is the
canonical "fan-in from multiple Traefiks into one provider container" shape.

### `provider-services.yaml.j2` (Jinja template)

The input file for the `YamlFile` provider above — entries for everything that isn't a
container DSM can scrape: IPMI, Proxmox hosts, Proxmox Backup Server, PiKVM, UniFi, a
bare-metal Plex install, an offsite Home Assistant. Two patterns worth highlighting:

- **Dual-URL via Tailscale**: offsite services have a primary URL plus a Tailscale-tailnet
  URL using `{{ tailscale_tailnet }}`, so the same dashboard works whether the user is on
  the home network or roaming.
- **`service_defaults_name`**: lets a service entry pick up the shipped defaults for a
  *different* name. Useful when a service runs under a customized name but you still want
  the bundled icon / category for the canonical app. Full YAML-file provider reference in
  [providers.md](providers.md#yaml-file).

### `homepage-service-widgets.yaml.j2` (Jinja template)

The widgets file referenced from `manager-config.yaml`. The interesting shape here is
**multiple instances of the same widget keyed off `server:`** — three AdGuard Home
instances, three Proxmox hosts — matching the [match precedence rules](managers.md#homepage-service-widgets)
the manager uses to pick a widget per service entry:

```yaml
- adguardhome:
    server: network-01
    widget:
      type: adguard
      url: https://adguardhome.{{ domain_name }}
      username: "{{ network_01_adguardhome_username }}"
      password: "{{ network_01_adguardhome_password }}"
- adguardhome:
    server: pi-rack
    widget:
      ...
```

Every credential (`username`, `password`, `key`) is a Jinja variable populated from an
Ansible vault at deploy time — never hand-edited into the rendered file.

## Adapting the example

Don't copy these files literally — every Jinja var (`{{ domain_name }}`,
`{{ tailscale_tailnet }}`, every secret) is populated from the maintainer's Ansible
inventory and vault. To adapt:

- **Pre-render manually**: substitute your own domain, hostnames, and credentials and
  drop the result in as plain YAML. Fine for a small homelab.
- **Use your own templating tool**: Ansible, `envsubst`, Helm, Kustomize — whatever fits
  your environment. The DSM config shapes don't care how the templating happens.
- **Keep secrets out of git** regardless of approach. The widgets file in particular needs
  API keys / passwords — vault them, don't commit them.
