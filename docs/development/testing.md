# Testing conventions

How tests in this repo are organized and which patterns to follow when adding new ones. Two test projects — `Dsm.Providers.Tests` and `Dsm.Managers.Tests` — share infrastructure via `Dsm.Shared.Tests`.

## Project layout

Each test project follows the same shape:

```
Dsm.{Project}.Tests/
├── {Project}TestFixtureHostedTestBase.cs   Project-specific defaults for the shared-DI host base
├── Usings.cs                                global usings (NUnit.Framework, Dsm.Shared.Tests)
├── UnitTests/                               Test fixtures live here — one per file, named <SUT>Tests
└── TestData/                                Static fixture files (YAML/JSON), copied to output
```

The per-project base resolves to [ProviderTestFixtureHostedTestBase](../../src/Dsm.Providers.Tests/ProviderTestFixtureHostedTestBase.cs) in `Dsm.Providers.Tests` and [ManagerTestFixtureHostedTestBase](../../src/Dsm.Managers.Tests/ManagerTestFixtureHostedTestBase.cs) in `Dsm.Managers.Tests`.

Static fixture data lives in `TestData/` at the project root. Reference it via `TestDataUtilities.GetTestDataPath("foo.yml")`.

## Shared infrastructure (`Dsm.Shared.Tests`)

The shared project carries cross-project test infrastructure. It's already imported via `global using Dsm.Shared.Tests;` in each test project's `Usings.cs`, so individual fixtures don't need their own `using`.

| Class | Purpose |
|---|---|
| [TestHost](../../src/Dsm.Shared.Tests/TestHost.cs) | Static factory: `TestHost.Create(...)` returns a disposable `IHost`. |
| [TestFixtureHostedTestBase](../../src/Dsm.Shared.Tests/TestFixtureHostedTestBase.cs) | Abstract base for fixture-shared DI (one host per fixture, disposed in `[OneTimeTearDown]`). Per-project subclasses add defaults. |
| [PerTestHostedTestBase](../../src/Dsm.Shared.Tests/PerTestHostedTestBase.cs) | Abstract base for per-test DI (`CreateHost(...)` helper that tracks hosts for `[TearDown]` disposal). |
| [TestTempDir](../../src/Dsm.Shared.Tests/TestTempDir.cs) | Disposable temp directory helper. |
| [TestTimeouts](../../src/Dsm.Shared.Tests/TestTimeouts.cs) | Constants for `[CancelAfter]`. |
| [TestDataUtilities](../../src/Dsm.Shared.Tests/TestDataUtilities.cs) | Path helper for `TestData/` fixture files. |

## DI in tests

Pick by asking: **does the DI graph vary across tests in this fixture?**

### Fixture-shared DI — DI graph is the same for every test

Extend the project's per-fixture host base:

```csharp
[CancelAfter(TestTimeouts.HungThresholdMs)]
public class FooTests : ProviderTestFixtureHostedTestBase
{
    [Test]
    public void X()
    {
        var sut = ServiceProvider.GetRequiredService<MyService>();
        // ...
    }

    // Optional: override AddServices to inject test-specific options/mocks.
    // Call base.AddServices(services) to keep the project's default registrations.
    protected override void AddServices(IServiceCollection services)
    {
        base.AddServices(services);
        services.AddTransient<IFoo>(_ => new FakeFoo());
    }
}
```

The host is built once in `[OneTimeSetUp]` (inside [TestFixtureHostedTestBase](../../src/Dsm.Shared.Tests/TestFixtureHostedTestBase.cs)) and disposed in `[OneTimeTearDown]`. The per-project subclass adds the project's default service registrations:

- `ProviderTestFixtureHostedTestBase` → `services.AddDsmProviderServices()`
- `ManagerTestFixtureHostedTestBase` → `services.AddDsmManagerServices()` + `configurationBuilder.AddDsmManagerConfiguration()`

Use this for integration-style tests that exercise the full DI graph. Examples: [DockerServicesProviderTests](../../src/Dsm.Providers.Tests/UnitTests/DockerServicesProviderTests.cs), [IconResolverTests](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs), [ServiceFactoryTests](../../src/Dsm.Managers.Tests/UnitTests/ServiceFactoryTests.cs).

### Per-test DI — DI graph depends on per-test state

Extend [PerTestHostedTestBase](../../src/Dsm.Shared.Tests/PerTestHostedTestBase.cs). It exposes a `CreateHost(...)` helper that builds an `IHost` and registers it for `[TearDown]` disposal. Fixture-specific registrations stay in fixture-specific helpers:

```csharp
[CancelAfter(TestTimeouts.HungThresholdMs)]
public class FooTests : PerTestHostedTestBase
{
    [Test]
    public async Task SomeTest()
    {
        using var dir = TestTempDir.Create("dsm_foo");
        var sut = BuildService(dir.Path);
        // ...
    }

    private MyService BuildService(string configPath)
    {
        var host = CreateHost(configureServices: services =>
        {
            services.AddDsmManagerServices();
            services.Configure<ManagerOptions>(opts => /* per-test config */);
        });
        return host.Services.GetRequiredService<MyService>();
    }
}
```

The base handles the disposable list and `[TearDown]` cleanup; the fixture only writes registrations. Multiple `CreateHost` calls per test are fine — they're disposed in reverse order. Examples: [DashboardCommandProcessorTests](../../src/Dsm.Managers.Tests/UnitTests/DashboardCommandProcessorTests.cs), [DashyDashboardManagerTests](../../src/Dsm.Managers.Tests/UnitTests/DashyDashboardManagerTests.cs), [HomepageDashboardManagerWidgetMatchingTests](../../src/Dsm.Managers.Tests/UnitTests/HomepageDashboardManagerWidgetMatchingTests.cs).

### Escape hatch — `using var TestHost.Create`

For a single one-off test that doesn't fit either base class, build a host inline:

```csharp
[Test]
public async Task SomeTest()
{
    using var dir = TestTempDir.Create("dsm_x");
    using var host = TestHost.Create(configureServices: services => { /* registrations */ });
    var sut = host.Services.GetRequiredService<MyService>();
}
```

### When to skip DI entirely

When the SUT is a pure transformation that's easier to assemble by hand — parser tests against a static method, or fixtures like [HomepageSettingsLayoutTests](../../src/Dsm.Managers.Tests/UnitTests/HomepageSettingsLayoutTests.cs) that construct the SUT with `NullLogger.Instance` and a hand-rolled stub. If you're tempted to override DI registrations to suppress side effects (logger, HTTP, network probes), construct directly instead.

## Mocking

The two projects deliberately follow different conventions because their dependency shapes differ.

**`Dsm.Providers.Tests` uses `Moq`** for outer-edge dependencies — `IDockerClient`, `ITraefikApiClient`, `ITraefikApiClientFactory`. The rest of the SUT graph wires up through `ProviderTestFixtureHostedTestBase`'s real DI. Examples: [DockerServicesProviderTests](../../src/Dsm.Providers.Tests/UnitTests/DockerServicesProviderTests.cs), [TraefikServicesProviderTests](../../src/Dsm.Providers.Tests/UnitTests/TraefikServicesProviderTests.cs).

**`Dsm.Managers.Tests` uses hand-rolled stubs** instead of `Moq` (and doesn't reference the `Moq` package at all). Manager-side code is mostly data transformations — the collaborator graph is shallow enough that a six-line `sealed class StubIconSource : IDashboardIconSource` is clearer than a `Mock<>` setup. Stub examples: [HomepageSettingsLayoutTests.StubIconSource](../../src/Dsm.Managers.Tests/UnitTests/HomepageSettingsLayoutTests.cs), [IconResolverTests.StubDashboardManager](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs).

If a manager test grows enough collaborator setup that hand-rolled stubs get noisy, treat that as a signal the code-under-test is doing too much — refactor the SUT before reaching for Moq.

## Filesystem fixtures

Use [TestTempDir.Create("prefix")](../../src/Dsm.Shared.Tests/TestTempDir.cs) — a disposable that creates `Path.GetTempPath()/prefix_{guid}` and recursively deletes on `Dispose`. Either `using var` per test, or a class-level field disposed in `[TearDown]`. The helper exposes `Path` and `RootedPath(relative)` for composing file paths inside.

Don't roll your own `Path.GetTempPath()` + `Directory.CreateDirectory` + ad-hoc cleanup — that pattern leaks temp dirs on test failure and obscures what each test owns.

## Network-dependent tests

Tests that hit a public CDN (currently only `cdn.jsdelivr.net`, for icon-source fallback probes) **must** be tagged `[Category("Network")]`. The dev workflow filters them out for offline runs:

```sh
dotnet test --solution src/Dsm.sln --filter "TestCategory!=Network"
```

If you add a test that calls `JsDelivrIconSource` (or any other `IDashboardIconSource` whose backing HTTP client hits a real host), tag it. [IconResolverTests](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs) is the working reference — the same fixture interleaves tagged and untagged tests based on whether the path under test reaches the CDN.

## Hung-test protection

Every fixture class carries `[CancelAfter(TestTimeouts.HungThresholdMs)]` (30 s, defined in [TestTimeouts.cs](../../src/Dsm.Shared.Tests/TestTimeouts.cs)). NUnit cancels the test's `CancellationToken` past the threshold, so a wedged HTTP call or infinite loop fails the suite instead of stalling it.

Apply the same attribute to any new fixture class. Don't put it on a base class — NUnit's `CancelAfterAttribute` is declared `Inherited = false`, so it doesn't propagate to subclass test methods.

## Naming

**`[TestFixture]` is omitted.** NUnit auto-detects fixtures from `[Test]` methods, so the explicit attribute is noise. New fixtures should start with `[CancelAfter(TestTimeouts.HungThresholdMs)]` directly above `public class FooTests`.

**Test method names** use `MethodOrScenario_Condition_ExpectedResult` PascalCase — e.g. `NativePrefix_PassesThroughWithoutTranslation`, `ListServices_FiltersAndMapsRouters`, `ExtractFirstHost_ReturnsNull_WhenNoHost`. Avoid single-word names like `Test`; the test runner shows the method name on failure, so it should describe the scenario well enough to navigate from a failing CI line.

## What "green" means

- A green run on `dotnet test --solution src/Dsm.sln --filter "TestCategory!=Network"` is the contract for offline correctness. The non-network suite must always pass.
- Network-tagged tests are allowed to fail when the CDN is unreachable.
- Flakes against `cdn.jsdelivr.net` (rate limits, transient 403s) are expected on the smoke tests in [IconResolverTests.FallbackChain_HomarrLabsIcons_Smoke](../../src/Dsm.Managers.Tests/UnitTests/IconResolverTests.cs) and indicate upstream availability, not a regression.
