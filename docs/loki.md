# Forwarding logs to Grafana Loki

Both services emit logs to the console by default. To forward those logs to a Grafana Loki
stack, add a `GrafanaLoki` sink via configuration. There's nothing to install separately — the
sink ships with the images and stays inert until you configure it.

## Enable via YAML

The recommended path: drop a `Serilog` block into `manager-config.yaml` (or
`provider-config.yaml` for the provider container):

```yaml
Serilog:
  WriteTo:
    - Name: GrafanaLoki
      Args:
        uri: http://loki:3100
        labels:
          - key: app
            value: dsm-manager-api
          - key: env
            value: prod
```

The console sink keeps running — adding the Loki sink is additive, not a replacement. Logs
appear on stdout and in Loki simultaneously.

## Enable via environment variables

Useful when you'd rather drive everything from the compose file:

```
Serilog__WriteTo__0__Name=GrafanaLoki
Serilog__WriteTo__0__Args__uri=http://loki:3100
Serilog__WriteTo__0__Args__labels__0__key=app
Serilog__WriteTo__0__Args__labels__0__value=dsm-manager-api
```

The `DSM_`-prefixed form (`DSM_Serilog__WriteTo__0__Name=GrafanaLoki`) also works.

## Labels

Static labels are key/value pairs under `labels`, as shown above. You can also attach dynamic
labels from log properties with `propertiesAsLabels`:

```yaml
Serilog:
  WriteTo:
    - Name: GrafanaLoki
      Args:
        uri: http://loki:3100
        propertiesAsLabels:
          - SourceContext
```

Each log line then carries a `SourceContext` label in Loki (e.g. the originating component's
fully-qualified name), letting you filter to one source without scanning all output.

## Failure handling

The sink batches log entries and pushes them over HTTP. If Loki is briefly unreachable the
sink buffers and retries; sustained outages drop entries rather than crash the host. Console
output is unaffected either way — your stdout logs keep flowing through your normal compose
log driver.

## More

- [configuration.md](configuration.md) — general overview of putting non-app-specific
  settings into the YAML config files.
- [Serilog.Sinks.Grafana.Loki](https://github.com/serilog-contrib/serilog-sinks-grafana-loki)
  — full reference for available `Args`.
