# Testing conventions

How tests are organized in this repo and which patterns to follow when adding new ones. The two test projects — `Dsm.Providers.Tests` and `Dsm.Managers.Tests` — share infrastructure via `Dsm.Shared.Tests`.

## Project layout

Both test projects use the same shape:

```
Dsm.{Project}.Tests/
├── BaseTest.cs                  Optional base for tests that need DI
├── Usings.cs                    global usings (NUnit.Framework, Dsm.Shared.Tests)
├── UnitTests/                   All [TestFixture] classes live here
│   └── *Tests.cs                One fixture per file, named <SUT>Tests
└── TestData/                    Static fixture files (YAML/JSON) — copied to output
```

Both projects keep static fixture files in `TestData/` at the project root (not under `UnitTests/`). Reference them from a test via `TestDataUtilities.GetTestDataPath("foo.yml")`.

The `Dsm.Shared.Tests` project carries the cross-project test infrastructure: `ServiceProviderFactory`, `TestDataUtilities`, `TestTimeouts`. Import these via the `global using Dsm.Shared.Tests;` already declared in each test project's `Usings.cs` — individual fixtures don't need their own `using`.

## When to extend `BaseTest`

`BaseTest` (per project) builds a real DI container by calling `ServiceProviderFactory.Create(ConfigureConfiguration, AddServices)` in `[OneTimeSetUp]`, then exposes the `IServiceProvider` to the fixture. Subclasses override `AddServices` / `ConfigureConfiguration` to inject test-specific options or mocks.

**Extend `BaseTest`** when the test exercises the full DI graph — e.g. an integration test resolving a `ServicesProviderFactory` and walking its dependencies, or a manager test that needs the real `IconResolver` and registered icon sources.

**Don't extend `BaseTest`** when the test is a focused transformation that's easier to assemble by hand — e.g. parser tests against a static method, or fixtures like `HomepageSettingsLayoutTests` that construct the SUT with `NullLogger.Instance` and a stub icon source to keep inputs predictable.

The rule of thumb: if you're tempted to override DI registrations to suppress side effects (logger, HTTP, network probes), construct directly instead.

## Mocking

The two projects deliberately follow different conventions because their dependency shapes differ.

**`Dsm.Providers.Tests` uses `Moq`** for outer-edge dependencies — `IDockerClient`, `ITraefikApiClient`, `ITraefikApiClientFactory`. The SUT graph is otherwise wired up through `BaseTest`'s real DI. Examples: [DockerServicesProviderTests.cs](../../src/Dsm.Providers.Tests/UnitTests/DockerServicesProviderTests.cs), [TraefikServicesProviderTests.cs](../../src/Dsm.Providers.Tests/UnitTests/TraefikServicesProviderTests.cs).

**`Dsm.Managers.Tests` uses hand-rolled stubs** rather than `Moq` (and does not reference the `Moq` package). Manager-side code is largely transformations on data — the collaborator graph is shallow enough that a six-line `sealed class StubIconSource : IDashboardIconSource` is clearer than a `Mock<IDashboardIconSource>` setup. Examples of the stub pattern: [HomepageSettingsLayoutTests.StubIconSource](../../src/Dsm.Managers.Tests/UnitTests/HomepageSettingsLayoutTests.cs), [IconResolverTests.StubDashboardManager](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs).

If a manager test grows enough collaborator setup that hand-rolled stubs become noisy, that's a signal the code-under-test is doing too much — refactor the SUT before reaching for `Moq`.

## Filesystem fixtures use `TestTempDir`

Fixtures that need a temp directory should construct one via [TestTempDir.Create("prefix")](../../src/Dsm.Shared.Tests/TestTempDir.cs) — a disposable that creates `Path.GetTempPath()/prefix_{guid}` and recursively deletes on `Dispose`. Use either `using var` per test or a class-level field paired with `[TearDown]` cleanup; both shapes are in active use. The helper exposes `Path` and `RootedPath(relative)` for composing file paths inside.

Don't reach for `Path.GetTempPath()` + `Directory.CreateDirectory` + ad-hoc `[TearDown]` logic directly — that pattern was unified into `TestTempDir` precisely because it leaks temp dirs on test failure and obscures what each test actually owns.

## `[TestFixture]` is omitted

NUnit auto-detects fixtures from `[Test]` methods, so the explicit `[TestFixture]` attribute is noise. New fixtures should start with the class declaration directly (preceded only by `[CancelAfter(...)]`).

## Network-dependent tests

Tests that hit a public CDN (currently only `cdn.jsdelivr.net` for icon-source fallback probes) **must** be tagged `[Category("Network")]`. The dev workflow filters them out for offline runs:

```sh
dotnet test --solution src/Dsm.sln --filter "TestCategory!=Network"
```

If you add a test that calls into `JsDelivrIconSource` (or any `IDashboardIconSource` whose backing HTTP client hits a real host), tag it. See [IconResolverTests.cs](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs) for the working pattern — the same fixture interleaves tagged and untagged tests based on whether the path under test reaches the CDN.

## Hung-test protection

Every fixture class carries `[CancelAfter(TestTimeouts.HungThresholdMs)]` (30 s, defined in [TestTimeouts.cs](../../src/Dsm.Shared.Tests/TestTimeouts.cs)). NUnit cancels the test's `CancellationToken` past the threshold so a wedged HTTP call or infinite loop fails the suite instead of stalling it.

Apply the same attribute to any new fixture class. Don't put it on `BaseTest` — NUnit's `CancelAfterAttribute` is declared `Inherited = false`, so it doesn't propagate to subclass test methods.

## Test method naming

`MethodOrScenario_Condition_ExpectedResult` PascalCase — e.g. `NativePrefix_PassesThroughWithoutTranslation`, `ListServices_FiltersAndMapsRouters`, `ExtractFirstHost_ReturnsNull_WhenNoHost`. Avoid single-word names like `Test`; the test runner shows the method name on failure, so it should describe the scenario well enough to navigate from a failing CI line.

## When tests should fail
- A green test run on `dotnet test --solution src/Dsm.sln --filter "TestCategory!=Network"` is the contract for offline correctness.
- Network-tagged tests are allowed to fail when the CDN is unreachable, but the *non-network* suite must always be green.
- Flakes against `cdn.jsdelivr.net` (rate limits, transient 403s) are expected on the smoke tests in [IconResolverTests.FallbackChain_HomarrLabsIcons_Smoke](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs) and indicate upstream availability, not a regression in our code.
