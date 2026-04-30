# Icon resolution

How a `Service.Icon` value (set by the user, by per-service defaults, or left empty) gets
turned into the `icon` / `image-url` field that lands in a dashboard's YAML.

End-user view: [docs/managers.md](../managers.md#per-service-defaults).

## Where the logic lives

| File | Role |
|---|---|
| [`ServiceWithDefaultsFactory`](../../src/Dsm.Managers/Services/ServiceWithDefaultsFactory.cs) | Per-service defaults + `ImagePath` resolution. Runs **once** per posted service. Doesn't touch icon prefixes. |
| [`IconResolver`](../../src/Dsm.Managers/Services/IconResolver.cs) | Per-manager prefix matching, CDN probing, native pass-through, fallback chain. Runs **per dashboard manager** during `WriteServices`. |
| [`IDashboardManager.NativeIconSourcePrefixes`](../../src/Dsm.Managers/DashboardManagers/IDashboardManager.cs) | Per-manager declaration: which `DashboardIconSourceType` values render natively, and the prefix the dashboard expects in its YAML. |
| [`IDashboardIconSource`](../../src/Dsm.Managers/Services/IconSources/IDashboardIconSource.cs) | One per source. `Type`, `Prefix`, `GetIconUrl(name) -> (Url, MatchedName)`. |
| [`JsDelivrIconSource`](../../src/Dsm.Managers/Services/IconSources/JsDelivrIconSource.cs) | Shared base for jsDelivr-served sources. Owns the cache, the HEAD-probe loop, and the name-normalization variants. |

The fan-out: `Service` flows out of the factory in user-facing form (`mdi-account`,
`hl-jellyfin`, …). Each manager's `WriteServices` runs an async pre-pass through every service,
calling `IconResolver.Resolve(service, this)`. The resulting `(Icon, ImageUrl)` tuple is what
gets written into the dashboard YAML.

## Precedence

Mirrors the pre-refactor factory behaviour exactly. Step numbers refer to the in-order checks
inside [`IconResolver.Resolve`](../../src/Dsm.Managers/Services/IconResolver.cs):

1. **`Icon` has a registered prefix** (`hl-` / `sh-` / `mdi-`):
   - Manager natively supports the source → return `(<native-prefix><iconName>, null)`. **No
     CDN probe.** The user vouched for the icon by typing the prefix.
   - Manager doesn't natively support → CDN probe via `IDashboardIconSource.GetIconUrl`.
     - Hit → return `(null, <Url>)`.
     - Miss → fall through to step 2.
2. **`ImageUrl` is set** (set by the caller, or by the factory's `ImagePath` resolution) →
   return `(null, <ImageUrl>)`.
3. **`Icon` is set without a registered prefix** (or with a prefix that probe-missed) →
   return `(<Icon>, null)`. Pass-through.
4. **Both `Icon` and `ImageUrl` empty** → walk `FallbackIconSourceProviders` in order:
   - Probe each entry (unconditionally — see "Why probe even for native sources" below).
   - Hit: native source → `(<native-prefix><matchedName>, null)`; non-native → `(null, <Url>)`.
   - All miss → `(null, null)`. The dashboard's own no-icon behaviour kicks in (Dashy stamps
     `favicon-local`; Homepage shows a generic placeholder).

The "fall-through on prefix miss" in step 1 is what gives a service with both `Icon: hl-FOO`
(probe-miss) and `ImageUrl: /local.svg` the ImageUrl on output instead of the literal
`hl-FOO` string — same precedence the old factory had.

## Why `MatchedName` exists

`IDashboardIconSource.GetIconUrl` returns *both* the resolved URL and the variant of the name
that hit the CDN:

```csharp
Task<(string? Url, string? MatchedName)> GetIconUrl(string iconName);
```

The base class probes a few normalized variants of the input — lowercase, spaces stripped,
spaces → hyphens, dots → hyphens — and the first 200 wins. The variant that won is the one
the dashboard's own renderer needs to look up. Without it, fallback-chain native pass-through
would stamp the user's literal service name (e.g. `Home Assistant`) which Homepage would 404
on; with it we stamp `home-assistant`.

For explicit prefixes (step 1), the user already typed the canonical name, so the matched
variant isn't needed there.

## Why probe even for native sources in the fallback chain

The earlier draft of this resolver skipped the probe entirely when a fallback source was
native to the manager — write `<native-prefix><serviceName>` and trust the dashboard to
handle a 404 gracefully. That worked for Homepage (graceful generic-icon fallback) but not
Dashy: `icon: hl-X` is a direct `<img>` to jsdelivr; on miss, you get a broken-image
placeholder.

So: fallback-chain entries always probe, regardless of native support. Native support only
changes what gets stamped *on a hit*.

## Native source declarations (shipped backends)

| Source | Dashy native prefix | Homepage native prefix |
|---|---|---|
| `HomarrLabs` (`hl-`) | `hl-` | `""` (Homepage's default lookup) |
| `SelfhSt` (`sh-`) | `sh-` | `sh-` |
| `MaterialDesignIcons` (`mdi-`) | `mdi-` | `mdi-` |

Net effect under the shipped backends: every prefix-driven icon passes through without a CDN
probe. Probes only fire for fallback-chain entries with empty `Icon`+`ImageUrl`, or for a
hypothetical custom backend that declines a source.

## Caching

Each `IDashboardIconSource` instance is registered as a singleton, so its cache (a
`ConcurrentDictionary<string, (Url, MatchedName, CachedAt)>`) lives for the process lifetime.

- **Hits** are cached forever.
- **Misses** are cached for 7 days
  ([`JsDelivrIconSource.NegativeCacheTtl`](../../src/Dsm.Managers/Services/IconSources/JsDelivrIconSource.cs)).
  Restart the manager to clear the negative cache (e.g. when an icon you uploaded becomes
  available).

The cache key is the *unnormalized* `iconName` argument the resolver passes in, so the same
icon requested twice (regardless of casing or spacing) won't re-probe.

## Adding a new `IDashboardIconSource`

1. Add a variant to [`DashboardIconSourceType`](../../src/Dsm.Managers/Services/IconSources/IDashboardIconSource.cs).
2. If your source is jsDelivr-backed and uses the same path scheme as the existing ones,
   extend [`JsDelivrIconSource`](../../src/Dsm.Managers/Services/IconSources/JsDelivrIconSource.cs)
   and override `Type`, `Prefix`, `BaseUrl` (the URL prefix up to and including the trailing
   `/`), `Extension` (without the dot), and `HttpClientName`. Otherwise implement
   `IDashboardIconSource` directly.
3. Register in [`HostBuilderExtensions`](../../src/Dsm.Managers/HostBuilder/HostBuilderExtensions.cs):
   ```csharp
   services.AddSingleton<IDashboardIconSource, YourSource>();
   services.AddHttpClient(YourSource.ClientName, c => c.Timeout = TimeSpan.FromSeconds(5));
   ```
4. Decide whether each existing dashboard manager renders this source natively. If so, add an
   entry to that manager's `NativeIconSourcePrefixes` mapping the new
   `DashboardIconSourceType` to whatever prefix the dashboard expects in its YAML (or `""` for
   a default lookup).
5. Optionally opt the source into the auto-fallback chain by adding it to
   `FallbackIconSourceProviders` in [`src/service-defaults.yaml`](../../src/service-defaults.yaml).

## Adding native prefix support for a new dashboard manager

Set `NativeIconSourcePrefixes` on the manager class:

```csharp
public IReadOnlyDictionary<DashboardIconSourceType, string> NativeIconSourcePrefixes { get; }
    = new Dictionary<DashboardIconSourceType, string>
    {
        [DashboardIconSourceType.HomarrLabs] = "",
        [DashboardIconSourceType.MaterialDesignIcons] = "mdi-",
    };
```

The dictionary value is the prefix the dashboard's icon renderer expects in its YAML. An empty
string means the dashboard does a default lookup with no prefix.

Sources you don't list here will fall through to CDN probing whenever a user types their
prefix — that's the right behaviour for any source the dashboard can't render itself.

## Category icons

Dashy section icons and Homepage layout (settings.yaml) icons go through the resolver too,
via a sibling method:

```csharp
public Task<(string? Icon, string? ImageUrl)> ResolveIcon(string? icon, IDashboardManager manager);
```

Behaviour matches `Resolve(Service, manager)` for the prefix-match step (native pass-through
or CDN probe; on probe miss return verbatim) but **omits the fallback chain**. A category
with no `Icon` set in `ServiceDefaultOptions.Categories.<name>.Icon` returns `(null, null)`
and the manager simply doesn't write a section/layout icon for it — same as today.

So:

| `Categories.media.Icon` | Dashy `conf.yml` section icon | Homepage `settings.yaml` layout icon |
|---|---|---|
| `mdi-multimedia` | `mdi-multimedia` | `mdi-multimedia` |
| `hl-jellyfin` | `hl-jellyfin` | `jellyfin` (translated to default lookup) |
| `https://my.cdn/foo.png` | `https://my.cdn/foo.png` | `https://my.cdn/foo.png` |
| `(unset)` | (no icon) | (no layout entry written) |

Per-manager wiring:
- [`DashyDashboardManager.ResolveSectionIcons`](../../src/Dsm.Managers/DashboardManagers/DashyDashboardManager.cs)
  pre-resolves a `Dictionary<string, string?>` keyed by title-cased section name before
  building the `DashySection` list.
- [`HomepageDashboardManager.UpdateSettingsLayoutIcons`](../../src/Dsm.Managers/DashboardManagers/HomepageDashboardManager.cs)
  resolves each distinct group inline; categories whose resolution returns `(null, null)`
  are filtered out.

## Tests

[`IconResolverTests`](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs) covers:
- Native pass-through (no probe) for each prefix.
- Prefix translation when manager and DSM use different prefixes (`hl-jellyfin` →
  `jellyfin` for Homepage's default lookup).
- Non-native fallback to CDN URL.
- Prefix-miss fall-through to ImageUrl.
- Native-prefix wins over ImageUrl (precedence regression test).
- Fallback chain: native source probes-then-stamps matched-name; non-native probes-then-uses
  URL; all-miss returns `(null, null)`.
- `ServiceDefaultsName` as the fallback lookup name.
- A `[TestCase]` smoke list of well-known Homarr Labs icons (jellyfin, plex, prometheus, …)
  to catch upstream renames or removals.

CDN tests are tagged `[Category("Network")]`. Skip with
`dotnet test --project src/Dsm.Managers.Tests --filter "TestCategory!=Network"` for offline runs.
