# Observability

Bondstone's diagnostics direction is OpenTelemetry-native: activities, tags,
metrics, structured logs, and clear misconfiguration errors should make the
durable module boundary observable without turning Bondstone into a broker
monitoring stack.

This page describes current behavior only. It also names planned
instrumentation so consumers can see the direction without treating future
signal names as stable contracts.

## Current Surfaces

Bondstone currently has a receive `ActivitySource` named `Bondstone.Modules`.
Module command and event receive pipelines start consumer activities through
the internal receive telemetry helper. Current receive activity tags are:

- `bondstone.message_id`
- `bondstone.message_kind`
- `bondstone.message_type`
- `bondstone.source_module`
- `bondstone.target_module`
- `bondstone.handler_identity`

Receive activities use trace context from `DurableMessageEnvelope.TraceContext`
when a valid W3C `traceparent` value is present. Invalid trace parent values
fail receive with an argument exception.

Operation state can carry optional diagnostic context:

- module name;
- durable message type name;
- handler identity.

`IDurableOperationResultReader` includes that context in result diagnostics and
result deserialization failures when it is present. Old rows, manually-created
states, and databases that have not added nullable diagnostic columns can still
be read; diagnostics fall back to operation id and requested result type.

Bondstone exposes read-only operational inspection through:

- `IDurableOutboxInspector` for terminal outbox failures;
- `IDurableInboxInspector` for unprocessed inbox rows.

The hosted outbox worker logs unexpected dispatch-batch failures with event id
`1001` and name `DispatchBatchFailed`.

RabbitMQ and Azure Service Bus receive workers log native receive-worker
failures through `ILogger`. Broker delivery counts, retry state, dead-letter
state, topology, and infrastructure health remain provider-native or
application-owned diagnostics.

Startup and runtime validation errors intentionally name missing durable
composition pieces, such as missing module persistence, missing mappings,
missing dispatchers, duplicate module durable registrations, invalid durable
identity attributes, missing receive bindings, and missing or ambiguous
dispatch routes. These messages are diagnostic surfaces, but they are not yet
cataloged as a stable error-code vocabulary.

## Not Current Behavior

Bondstone does not yet expose finalized metrics for outbox claims, dispatches,
retries, terminal failures, receive outcomes, inbox decisions, operation
finalization, or operation expiration.

Bondstone does not yet publish a complete stable vocabulary for all activity
names, tags, metric names, log event ids, or misconfiguration codes.

Bondstone does not provide provider-neutral topology diagnostics, broker retry
diagnostics, dead-letter diagnostics, subscription storage diagnostics, or
broker monitoring. Use native broker clients, broker management surfaces,
application logs, and provider-specific telemetry for that layer.

## Planned Direction

Future instrumentation should prefer OpenTelemetry signals at durable
boundaries:

- durable send and publish;
- outbox claim, dispatch, retry, and terminal failure;
- receive, inbox decision, and handler execution;
- operation result completion, finalization, and expiration;
- serializer and deserialization failures where they affect durable payloads.

Future metrics and tags should use a small stable vocabulary before being
documented as contracts. Until that vocabulary exists, application code should
add its own domain-specific logs and metrics around endpoints, handlers,
broker receive loops, operation polling, and operator jobs.

Transport adapter packages may log native settlement handoff, but such logs
must not imply Bondstone owns broker retry, dead-letter, topology, or
monitoring policy.

For operational procedures and ownership boundaries, see
[operations.md](operations.md).
