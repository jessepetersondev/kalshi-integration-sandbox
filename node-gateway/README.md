# Node Gateway

Lightweight Node.js integration gateway and webhook simulator for the Kalshi Integration Sandbox.

## Structure

```text
src/
  clients/
  models/
  routes/
  services/
  config.js
  server.js

tests/
```

## Endpoints
- `GET /health`
- `POST /webhooks/simulate/execution-update`

## Purpose
- Simulate external Kalshi-style execution callbacks
- Validate inbound payload shape
- Forward valid execution updates to the .NET backend callback endpoint
- Keep the integration boundary clean and testable

## Run
```bash
node src/server.js
```

## Test
```bash
node --test
```
