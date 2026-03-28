# Event Publishing Extension Path

The sandbox now has a **clean application-boundary publishing abstraction** for outbound application events:

- `IApplicationEventPublisher` lives in `Kalshi.Integration.Application`
- `ApplicationEventEnvelope` defines the transport-agnostic event contract
- `InMemoryApplicationEventPublisher` is the current MVP implementation for in-process publication and tests

## Current MVP behavior

The current implementation is intentionally small:

- successful API workflows publish application events in-process
- the in-memory publisher keeps a local event history for inspection
- tests can subscribe to events directly without a broker
- no RabbitMQ / Azure Service Bus dependency is added yet

This keeps the portfolio project simple while still showing the correct extension seam.

## Publisher responsibilities

The publisher abstraction is responsible for:

- accepting a transport-agnostic application event envelope
- dispatching that event through the configured implementation
- avoiding any dependency on a concrete broker in application/domain code

The publisher abstraction is **not** responsible for:

- business validation
- workflow orchestration
- domain state transitions
- broker-specific retry policy semantics
- queue / topic provisioning

Those concerns stay in the appropriate layer.

## Current event shape

`ApplicationEventEnvelope` carries:

- event id
- category
- event name
- resource id
- correlation id
- idempotency key
- string-based attributes
- occurred-at timestamp

The envelope is intentionally generic so a future broker adapter can serialize it without pushing broker concepts into the application layer.

## RabbitMQ / Azure Service Bus future path

If this sandbox later needs real asynchronous messaging, keep the application contract unchanged and add a new infrastructure implementation:

- `RabbitMqApplicationEventPublisher`
- `AzureServiceBusApplicationEventPublisher`

Those adapters would be responsible for:

- mapping `ApplicationEventEnvelope` to broker messages
- choosing exchange / queue / topic names
- adding broker headers from correlation/idempotency metadata
- serialization
- publish retries / transient fault handling
- delivery telemetry

The application layer should still only know about `IApplicationEventPublisher`.

## Why no broker yet?

The current stories only require:

- a clean abstraction
- an in-memory implementation for MVP use
- easy testability
- documentation for the extension path

Adding RabbitMQ or Azure Service Bus now would be premature and would increase setup, infrastructure, and failure modes without improving the portfolio signal for this stage.
