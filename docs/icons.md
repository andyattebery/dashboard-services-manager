# Icons

Most services on your dashboard already get a sensible icon **automatically**, just from the
service's name. A service called "Jellyfin" gets the Jellyfin icon for free; same for
"Plex", "Sonarr", "Home Assistant", "Pi-hole", and most other common self-hosted apps. You
don't have to configure anything.

The rest of this doc covers the cases where the automatic pick isn't what you want — when
you'd like a specific icon, an image URL, or a category icon — and how the prefixes work.

## Automatic icons (the default)

When DSM writes your dashboard YAML, any service that doesn't have an explicit icon falls
through to a CDN lookup. The shipped defaults probe two icon collections in order, using
the service's name:

```yaml
ServiceDefaultOptions:
  FallbackIconSourceProviders:
    - HomarrLabs       # tried first — https://github.com/homarr-labs/dashboard-icons
    - SelfhSt          # tried next  — https://selfh.st/icons/
```

The first hit wins. The lookup is forgiving: case-insensitive, and DSM tries common name
variants automatically — so "Home Assistant" matches `home-assistant`, "Node.js" matches
`node-js`, and so on. If nothing matches, the dashboard renders without an icon (Dashy
shows its `favicon-local` fallback; Homepage shows a generic placeholder).

## When the automatic pick isn't what you want

Three reasons to set an icon yourself:

- **Wrong icon picked.** Two unrelated services share a name; or the icon collection has
  one called e.g. "plex" that isn't the Plex you mean. Override per-service.
- **Service has an unusual name.** Your "InternalAdminTool" container won't match any CDN
  icon. Set one explicitly.
- **You have a specific image in mind.** A custom logo URL, a path on the service itself,
  or a different CDN icon you prefer. Use an image URL or one of the prefixed lookups
  below.

## Setting an icon explicitly

Four input paths, each with the same effect on the dashboard YAML:

**Container label** on a Docker / Swarm container:

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    labels:
      - dsm.icon=hl-jellyfin
```

**Provider YAML file entry** (`provider-config.yaml`-managed YAML services file):

```yaml
- name: My Internal Tool
  url: https://internal.example
  icon: mdi-server-network
```

**Per-service override** in `manager-config.yaml`:

```yaml
ServiceDefaultOptions:
  Services:
    plex:
      Icon: my-custom-icon
```

**Category icon** in `manager-config.yaml`:

```yaml
ServiceDefaultOptions:
  Categories:
    media:
      Icon: mdi-multimedia
```

## CDN icon prefixes

Three prefixes, three icon collections:

| Prefix | Collection | Catalog |
|---|---|---|
| `hl-` | Homarr Labs dashboard-icons | <https://dashboard-icons.homarrlabs.com/> |
| `sh-` | selfh.st icons | <https://selfh.st/icons/> |
| `mdi-` | Material Design Icons | <https://pictogrammers.com/library/mdi/> |

Use the bare icon name from the collection — no extension, no path. Examples:

```yaml
Icon: hl-jellyfin
Icon: sh-sonarr
Icon: mdi-home-automation
```

The prefix is case-insensitive (`HL-jellyfin` works the same as `hl-jellyfin`).

Both Dashy and Homepage render all three prefixes natively, so when DSM writes the
dashboard YAML the prefix-form name passes straight through (translated to whatever the
dashboard expects — Homepage's default Homarr Labs lookup uses no prefix, so `hl-jellyfin`
ends up as `icon: jellyfin` in `services.yaml`). The dashboard fetches the icon itself; DSM
doesn't make any extra HTTP requests at write time.

## Image URLs

When you have a specific URL or local path you want to use:

**Container label** on a Docker / Swarm container:

```yaml
labels:
  - dsm.image_path=https://my.cdn/foo.png       # absolute URL
  - dsm.image_path=/static/img/logo.svg         # relative — joined to the service's URL
```

**Per-service override** in `manager-config.yaml`:

```yaml
ServiceDefaultOptions:
  Services:
    grafana:
      ImagePath: /public/img/grafana_icon.svg   # relative — joined to grafana's URL
```

Absolute URLs (`http(s)://...`) pass through unchanged. Relative paths are joined onto the
service's `Url`, so `/static/foo.png` on a service whose URL is `https://app.example` becomes
`https://app.example/static/foo.png` in the dashboard YAML.

## Customising or disabling the automatic-pick chain

Override `FallbackIconSourceProviders` in `manager-config.yaml` to change the order, drop a
source, or turn the auto-pick off entirely:

```yaml
ServiceDefaultOptions:
  FallbackIconSourceProviders:
    - SelfhSt          # try selfh.st first, skip Homarr Labs entirely
```

```yaml
ServiceDefaultOptions:
  FallbackIconSourceProviders: []   # opt out — services without an explicit icon stay iconless
```

The auto-pick runs only for services. A category with no `Icon` set in
`ServiceDefaultOptions.Categories.<name>.Icon` stays silent — DSM doesn't probe a chain to
fill it in.

## Category icons

Categories (Dashy section icons, Homepage `settings.yaml` layout icons) use the same prefix
vocabulary as service icons. Configure them under `ServiceDefaultOptions.Categories` in
`manager-config.yaml`:

```yaml
ServiceDefaultOptions:
  Categories:
    media:    { Icon: mdi-multimedia }
    network:  { Icon: mdi-network }
    custom:   { Icon: hl-jellyfin }
```

The shipped defaults cover the common categories — see the `Categories:` block in
[src/service-defaults.yaml](../src/service-defaults.yaml) for the full list. You can extend
or override any of them in `manager-config.yaml`.

## More

- [docs/managers.md](managers.md) — manager-side configuration, including
  `ServiceDefaultOptions` and how the dashboard YAML is written.
- [docs/providers.md](providers.md) — container labels (including `dsm.icon` and
  `dsm.image_path`) and the YAML file provider format.
- [docs/development/icons.md](development/icons.md) — the underlying mechanism: resolver
  precedence, CDN probing, caching, how to add a new icon source.
