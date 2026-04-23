# dotnet/

.NET port of the Ruby/Sinatra dashboard-services-manager in [../src/](../src/). Two deployables:
[`Dsm.Manager.Api`](Dsm.Manager.Api/) owns the Dashy dashboard file, and
[`Dsm.Provider.App`](Dsm.Provider.App/) discovers services in your infrastructure and POSTs them
to the API.

See:

- [docs/overview.md](docs/overview.md) — architecture, project map, data flow, build/run/test
- [docs/managers.md](docs/managers.md) — `Dsm.Managers` internals and the combiner rule
- [docs/providers.md](docs/providers.md) — `Dsm.Providers` + `Dsm.Provider.App` and how to add a provider

Quick start:

```sh
dotnet build Dsm.sln
dotnet run --project Dsm.Manager.Api
dotnet test Dsm.sln --filter "TestCategory!=Network"
```
