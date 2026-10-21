# Icon resolution

How a service's `Icon` and `ImageUrl` become the icon written into a dashboard's YAML.
For the end-user view, see [docs/icons.md](../icons.md).

## Where it runs

Icons are resolved separately for each dashboard, at the moment its file is written. Each
`IDashboardManager.WriteServices` calls
[`IconResolver.Resolve(service, this)`](../../src/Dsm.Managers/Services/IconResolver.cs) for
every service and writes the resulting `(Icon, ImageUrl)` pair. It has to happen per dashboard
because dashboards differ in which icon sets they render themselves. The same service is
written as `hl-jellyfin` for Dashy and as `jellyfin` for Homepage. Before this point, through
defaults and the merge, a service carries its icon exactly as the user or the defaults wrote
it, e.g. `hl-jellyfin`.

| Piece | Role |
|---|---|
| [`IconResolver`](../../src/Dsm.Managers/Services/IconResolver.cs) | Prefix matching, native pass-through, CDN probing, fallback chain. A singleton. |
| [`IDashboardManager.NativeIconSourcePrefixes`](../../src/Dsm.Managers/DashboardManagers/IDashboardManager.cs) | Each dashboard's declaration of the icon sources it renders itself, and the prefix it expects for each. |
| [`IDashboardIconSource`](../../src/Dsm.Managers/Services/IconSources/IDashboardIconSource.cs) | One per icon set: `Type`, `Prefix`, `GetIconUrl(name)` → `(Url, MatchedName)`. |
| [`JsDelivrIconSource`](../../src/Dsm.Managers/Services/IconSources/JsDelivrIconSource.cs) | Base class for the shipped sources. Name variants, HEAD probing, cache. |

The shipped sources are all served by jsDelivr:

| Source | Prefix | Probed at |
|---|---|---|
| `HomarrLabs` | `hl-` | `https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png/<name>.png` |
| `SelfhSt` | `sh-` | `https://cdn.jsdelivr.net/gh/selfhst/icons/png/<name>.png` |
| `MaterialDesignIcons` | `mdi-` | `https://cdn.jsdelivr.net/npm/@mdi/svg@latest/svg/<name>.svg` |

## Resolution order

`Resolve` returns at the first step that produces an answer:

1. **The icon has a registered prefix.** The prefix match is case-insensitive.
   - If the dashboard renders that source itself, the result is `(<native prefix><name>, null)`.
     No request is made: the user chose the icon by typing the prefix.
   - Otherwise DSM probes the source. On a hit the result is `(null, <CDN URL>)`. On a miss it
     moves on to the next step.
2. **`ImageUrl` is set** (by the provider, or from `ImagePath` in the defaults): `(null, ImageUrl)`.
3. **`Icon` is set** (no prefix, or a prefix whose probe missed): `(Icon, null)`, verbatim.
4. **Neither is set:** walk the [fallback chain](#the-fallback-chain). If nothing is found the
   result is `(null, null)`, logged as a warning.

Steps 1 and 2 interact. A service with `Icon: hl-foo` and `ImageUrl: /local.svg` keeps its
`hl-foo` icon wherever `hl-` is native. On a dashboard that doesn't render `hl-`, a probe miss
for `foo` hands over to the image URL.

Each dashboard turns the pair into its own icon field:
- **Dashy** writes `ImageUrl`, else `Icon`, else `favicon-local`, which tells Dashy to use the
  service's own favicon.
- **Homepage** writes `ImageUrl`, else `Icon`, and omits the field when both are empty.

## Native prefixes

| Source | Dashy writes | Homepage writes |
|---|---|---|
| `HomarrLabs` | `hl-<name>` | `<name>` (Homepage's default lookup is the same Dashboard Icons set) |
| `SelfhSt` | `sh-<name>` | `sh-<name>` |
| `MaterialDesignIcons` | `mdi-<name>` | `mdi-<name>` |

Both shipped dashboards render all three sources. A prefixed icon is therefore never probed
on its own: probes happen only in the fallback chain, or for a dashboard that leaves a source
out of its map.

## The fallback chain

A service that reaches step 4 has neither an icon nor an image URL. DSM looks for an icon by
name. The lookup name is the service's `ServiceDefaultsName` if set, otherwise its `Name`.
Each source listed in `ServiceDefaultOptions.FallbackIconSourceProviders` is probed in order;
the shipped list is `HomarrLabs`, then `SelfhSt` (see
[service-defaults.md](service-defaults.md#service-defaultsyaml)).

- **A hit on a source the dashboard renders itself** gives `(<native prefix><matched name>, null)`.
- **A hit on any other source** gives `(null, <CDN URL>)`.
- **A miss** moves on to the next source.

Every fallback source is probed, including ones the dashboard renders natively. A native
source changes only how a hit is written. The probe is what guarantees that a written name
exists in the set. Without it, DSM would write `hl-<service name>` for every unknown service,
and the dashboard would be left with a name that may not exist. Dashy, for one, fetches `hl-`
icons straight from jsDelivr.

### Matched names

`GetIconUrl` returns the URL that answered and also the variant of the name that matched:

```csharp
Task<(string? Url, string? MatchedName)> GetIconUrl(string iconName);
```

`JsDelivrIconSource` lower-cases the name and tries up to three variants, keeping the first
one that answers `2xx`:
1. spaces removed
2. spaces turned into hyphens
3. dots turned into hyphens

A native hit is written with the matched variant, because that is the form the dashboard's
own renderer looks up. "AdGuard Home" is written as `adguard-home`, not as the display name.
Step 1 doesn't need this: there the user typed the icon name.

## Caching

Each icon source is a singleton holding a cache keyed by the name exactly as it was passed in,
so `Plex` and `plex` are separate entries.
- Hits are kept for the life of the process.
- Misses expire after 7 days (`JsDelivrIconSource.NegativeCacheTtl`). To retry a miss sooner,
  for example after an icon is added upstream, restart the manager.

Each source's named `HttpClient` has a 5-second timeout
([HostBuilderExtensions.cs](../../src/Dsm.Managers/HostBuilder/HostBuilderExtensions.cs)).

## Category icons

Dashy section icons and Homepage layout icons come from `ServiceDefaultOptions.Categories.<name>.Icon`
and go through a sibling method:

```csharp
public Task<(string? Icon, string? ImageUrl)> ResolveIcon(string? icon, IDashboardManager manager);
```

`ResolveIcon` runs step 1 only. A native prefix passes through (translated); a non-native
prefix is probed. Anything else, including a probe miss, comes back verbatim. There is no
fallback chain: a category without a configured icon gets no icon.

| `Categories.media.Icon` | Dashy section icon (`conf.yml`) | Homepage layout icon (`settings.yaml`) |
|---|---|---|
| `mdi-multimedia` | `mdi-multimedia` | `mdi-multimedia` |
| `hl-jellyfin` | `hl-jellyfin` | `jellyfin` |
| `https://my.cdn/foo.png` | `https://my.cdn/foo.png` | `https://my.cdn/foo.png` |
| not set | no section icon | no layout entry |

The callers are `DashyDashboardManager.ResolveSectionIcons` and
`HomepageDashboardManager.UpdateSettingsLayoutIcons`. How categories are named and looked up
is in [managers.md](managers.md#categories).

## Adding an icon source

1. Add a value to [`DashboardIconSourceType`](../../src/Dsm.Managers/Services/IconSources/DashboardIconSourceType.cs).
2. Implement the source:
   - **A jsDelivr-hosted set with the same URL shape:** subclass `JsDelivrIconSource`.
     Override `Type`, `Prefix`, `BaseUrl` (everything before the name, ending in `/`),
     `Extension` (no dot) and `HttpClientName`, and expose a `ClientName` constant.
   - **Anything else:** implement `IDashboardIconSource` directly.

   Prefixes must not overlap: the resolver uses the first registered source whose prefix the
   icon starts with.
3. Register the source and its client in
   [HostBuilderExtensions.cs](../../src/Dsm.Managers/HostBuilder/HostBuilderExtensions.cs):
   ```csharp
   services.AddSingleton<IDashboardIconSource, YourSource>();
   services.AddHttpClient(YourSource.ClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
   ```
4. Add `YourSource.ClientName` to `ConfigureOfflineIconHttpClients` in
   [IconHttpClientStubs.cs](../../src/Dsm.Managers.Tests/IconHttpClientStubs.cs). Without it,
   manager tests that resolve icons through the real configuration can reach the live host
   ([testing.md](testing.md#mocking-and-stubs)).
5. For each dashboard that renders the set itself, add the type to that manager's
   `NativeIconSourcePrefixes`, mapped to the prefix the dashboard expects (`""` for a
   no-prefix lookup).
6. Optionally add the type to `FallbackIconSourceProviders` in `service-defaults.yaml`.

## Native support in a new dashboard

A new `IDashboardManager` declares the sources it renders, and the prefix each needs:

```csharp
public IReadOnlyDictionary<DashboardIconSourceType, string> NativeIconSourcePrefixes { get; }
    = new Dictionary<DashboardIconSourceType, string>
    {
        [DashboardIconSourceType.HomarrLabs] = "",
        [DashboardIconSourceType.MaterialDesignIcons] = "mdi-",
    };
```

A source left out of the map is treated as foreign to that dashboard. Its prefixed icons are
probed on the CDN and written as URLs, which is the right outcome for a set the dashboard
can't render.

## Tests

[`IconResolverTests`](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs) covers:
- native pass-through and prefix translation (`hl-jellyfin` → `jellyfin`)
- the CDN fallback for non-native prefixes, including the hit, the miss and the hand-over to
  `ImageUrl`
- native prefixes beating `ImageUrl`
- the fallback chain: native and non-native hits, all-miss, the empty chain, and
  `ServiceDefaultsName` as the lookup name
- `ResolveIcon`
- a smoke list of well-known Homarr Labs icons that catches upstream renames

Tests that reach the CDN are tagged `[Category("Network")]`
([testing.md](testing.md#network-dependent-tests)).
