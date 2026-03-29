# Observability

The Kalshi Integration Sandbox now includes an **OpenTelemetry-based observability baseline** for traces and metrics.

## What is instrumented

### Traces

The API emits distributed traces for:
- inbound ASP.NET Core requests
- outbound `HttpClient` calls
- EF Core database activity

This gives the repo coverage over the most important request path components:
- incoming client/API traffic
- important dependency calls
- persistence operations

### Metrics

The API emits metrics for:
- ASP.NET Core request activity
- outbound `HttpClient` activity
- runtime metrics
- process metrics
- custom request/dependency duration histograms

Custom application metrics currently include:
- `kalshi.http.server.request.duration`
- `kalshi.dependency.call.duration`

## Service identity

Telemetry is emitted under the configured OpenTelemetry service identity:

```json
"OpenTelemetry": {
  "ServiceName": "Kalshi.Integration.Api",
  "ServiceVersion": "v1"
}
```

The deployment environment is also attached as resource metadata.

## Configuration

OpenTelemetry settings live under:
- `OpenTelemetry`

Current configuration fields:
- `ServiceName`
- `ServiceVersion`
- `OtlpEndpoint`
- `AzureMonitorConnectionString`
- `EnableConsoleExporter`

Example:

```json
"OpenTelemetry": {
  "ServiceName": "Kalshi.Integration.Api",
  "ServiceVersion": "v1",
  "OtlpEndpoint": "http://otel-collector:4317",
  "AzureMonitorConnectionString": null,
  "EnableConsoleExporter": false
}
```

## OTLP export path

If `OpenTelemetry:OtlpEndpoint` is configured, the app exports both traces and metrics to that OTLP endpoint.

Typical examples:

```bash
export OpenTelemetry__OtlpEndpoint='http://localhost:4317'
```

or in cloud configuration:

```json
"OpenTelemetry": {
  "OtlpEndpoint": "http://otel-collector:4317"
}
```

This makes the repo compatible with:
- local OpenTelemetry Collector setups
- Grafana Tempo / Prometheus-style pipelines via collector routing
- vendor-neutral OTLP backends
- Azure Monitor / Application Insights via collector-based forwarding

## Azure Monitor / App Insights compatibility

The implementation is standards-based OpenTelemetry, which means it is compatible with Azure Monitor / Application Insights deployment paths.

Recommended Azure-oriented options:
- send OTLP to an OpenTelemetry Collector that forwards to Azure Monitor
- or add the Azure Monitor OpenTelemetry exporter directly in a future deployment-specific step

For this repo, the important part is that the instrumentation is **OpenTelemetry-native and export-path configurable** rather than being tied to a single vendor.

## Local validation idea

Run the API with an OTLP endpoint and inspect emitted traces/metrics through a collector-backed local stack.

Example environment variable:

```bash
export OpenTelemetry__OtlpEndpoint='http://localhost:4317'
```

Then start the app and exercise:
- `POST /api/v1/trade-intents`
- `POST /api/v1/orders`
- `GET /api/v1/orders/{id}`
- `GET /health/ready`

Those requests should produce inbound request telemetry, and dependency/database work should show up beneath the request path when the backend path exercises EF Core or outbound HTTP.

## Why this matters

This baseline makes the repo easier to discuss in production terms:
- request traces are correlation-friendly
- dependency behavior is visible
- metrics are ready for dashboards/alerts
- export stays portable across vendor choices
