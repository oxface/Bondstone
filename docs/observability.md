# Observability

Bondstone's diagnostics direction is OpenTelemetry-native: activities, tags,
metrics, structured logs, and clear misconfiguration errors should make the
durable module boundary observable without turning Bondstone into a broker
monitoring stack.

This page describes current behavior only. The broader diagnostics direction
is owned by the BMAD architecture artifact.

## Current Activity Sources

Bondstone currently emits activities from these `ActivitySource` names:

- `Bondstone.Modules` for module durable messaging boundaries;
- `Bondstone.Persistence` for provider-neutral durable persistence work.

Bondstone currently emits metrics from matching `Meter` names:

- `Bondstone.Modules` for module receive idempotency and operation metrics;
- `Bondstone.Persistence` for provider-neutral outbox dispatch metrics.

## Current Activities

`Bondstone.Modules` currently emits:

- `bondstone.command.send`, `Producer`: durable command send accepted into the
  current source module outbox;
- `bondstone.event.publish`, `Producer`: integration event publish accepted
  into the current source module outbox;
- `bondstone.module_command.receive`, `Consumer`: command envelope receive
  through a module command handler route;
- `bondstone.module_event.receive`, `Consumer`: event envelope receive through
  a module subscriber route;
- `bondstone.operation.finalize`, `Internal`: explicit operation finalization
  through application-owned policy.

`Bondstone.Persistence` currently emits:

- `bondstone.outbox.dispatch`, `Internal`: one provider-neutral outbox
  dispatch batch;
- `bondstone.outbox.dispatch.message`, `Internal`: one claimed outbox message
  handoff to the configured envelope dispatcher.

Receive activities use trace context from `DurableMessageEnvelope.TraceContext`
when a valid W3C `traceparent` value is present. Invalid trace parent values
fail receive with an argument exception. Send and publish activities capture
current trace context into the durable envelope unless the caller supplied an
explicit `MessageTraceContext`.

## Current Tags

Durable message activities can emit:

- `bondstone.message_id`;
- `bondstone.message_kind`;
- `bondstone.message_type`;
- `bondstone.source_module`;
- `bondstone.target_module`;
- `bondstone.partition_key`;
- `bondstone.operation_id`;
- `bondstone.handler_identity`, receive activities only.

Operation finalization activities can emit:

- `bondstone.module`;
- `bondstone.operation_id`;
- `bondstone.operation_status`;
- `bondstone.operation_finalized`.

Outbox dispatch activities can emit:

- `bondstone.outbox.claimed_by`;
- `bondstone.outbox.max_count`;
- `bondstone.outbox.claimed_count`;
- `bondstone.outbox.dispatched_count`;
- `bondstone.outbox.retry_scheduled_count`;
- `bondstone.outbox.terminal_failed_count`;
- `bondstone.outbox.stale_count`.

Outbox message dispatch activities can also emit durable message tags:

- `bondstone.message_id`;
- `bondstone.message_kind`;
- `bondstone.message_type`;
- `bondstone.source_module`;
- `bondstone.target_module`.

Activity status is set to `Error` when the instrumented Bondstone boundary
throws. Outbox message dispatch failures that are recorded for retry or
terminal failure set the message-dispatch activity to `Error`; the batch
activity records the resulting retry, terminal failure, or stale counts.

Tags whose source values are absent, such as event `target_module` or
operation ids on untracked messages, may be absent from the emitted activity.

## Current Metrics

Bondstone metrics are OpenTelemetry-native .NET counters. They count only
Bondstone-owned durable state transitions and intentionally avoid
high-cardinality labels such as message id, operation id, exception message,
payload data, broker delivery count, or native destination name.

`Bondstone.Persistence` currently emits:

- `bondstone.outbox.claimed`: outbox records claimed for dispatch;
- `bondstone.outbox.dispatched`: outbox records recorded as dispatched;
- `bondstone.outbox.retry_scheduled`: outbox records recorded for retry;
- `bondstone.outbox.terminal_failed`: outbox records recorded as terminal
  failed;
- `bondstone.outbox.stale`: claimed outbox records whose lease or outcome
  update was no longer owned by the dispatcher.

Outbox metric attributes can include:

- `bondstone.message_kind`;
- `bondstone.source_module`;
- `bondstone.target_module`, when present.

`Bondstone.Modules` currently emits:

- `bondstone.direct_receive.handled`: module receive completed handler
  execution and receive idempotency handling. The instrument keeps its
  transitional name until the diagnostics vocabulary is renamed.
- `bondstone.direct_receive.already_processed`: module receive found an
  already processed idempotency row and skipped handler execution
  idempotently.
- `bondstone.direct_receive.already_received`: module receive found an
  already received but unprocessed idempotency row and raised the ambiguous
  receive error.
- `bondstone.operation.finalized`: explicit operation finalizer wrote a new
  terminal operation state;
- `bondstone.operation.expiration.candidates`: operation expiration processing
  found stale pending or running candidates;
- `bondstone.operation.expiration.finalized`: operation expiration processing
  finalized candidates through the explicit finalizer.

Direct receive metric attributes can include:

- `bondstone.module`, meaning the target command module or subscriber module;
- `bondstone.message_kind`;
- `bondstone.source_module`;
- `bondstone.target_module`, when present.

Operation metric attributes can include:

- `bondstone.module`;
- `bondstone.operation_status`.

These metrics are intentionally not broker topology, queue health, native retry,
dead-letter, delivery-count, or provider monitoring signals. Use the selected
broker client, broker management plane, host logs, and application telemetry
for those layers.

## Current Result And Inspection Diagnostics

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

Startup and runtime validation errors intentionally name missing durable
composition pieces, such as missing module persistence, missing mappings,
missing dispatchers, duplicate module durable registrations, invalid durable
identity attributes, missing receive bindings, and missing or ambiguous
dispatch routes. These messages are diagnostic surfaces, but they are not a
stable error-code vocabulary.

## Current Logs

The hosted outbox worker logs unexpected dispatch-batch failures with event id
`1001` and name `DispatchBatchFailed`.

The hosted incoming inbox processing worker logs unexpected process-batch
failures with event id `2001` and name `ProcessBatchFailed`, including the
worker id and consecutive failure count.

RabbitMQ receive workers log receive failures with event id `2001` and name
`ReceiveFailed` before nacking according to the native `RequeueOnFailure`
option.

Azure Service Bus receive workers log processor failures with event id `3001`
and name `ReceiveFailed`. Native retry, settlement exhaustion, and
dead-letter behavior remain Azure Service Bus client and broker behavior.

Broker delivery counts, retry state, dead-letter state, topology, and
infrastructure health remain provider-native or application-owned
diagnostics.

## Not Current Behavior

Bondstone does not publish stable misconfiguration error codes. Startup and
runtime exception messages are intentionally clear, but they are not a
machine-readable error-code vocabulary.

Bondstone does not currently emit finalized durable inbox worker metrics. The
existing incoming inbox dispatcher emits activities and provisional
diagnostics for Bondstone-owned durable inbox transitions such as rows
claimed, processed, retry scheduled, terminal receive failed, and stale claim
or outcome updates. Future finalized metrics should use low-cardinality
attributes such as module, message kind, source module, target module when
present, status, and controlled transport diagnostic names.

Bondstone does not provide provider-neutral topology diagnostics, broker retry
diagnostics, dead-letter diagnostics, subscription storage diagnostics, or
broker monitoring. Use native broker clients, broker management surfaces,
application logs, and provider-specific telemetry for that layer.

Application code should add domain-specific logs and metrics around endpoints,
handlers, broker receive loops, operation polling, and operator jobs where the
current Bondstone surface does not expose the signal the application needs.

Transport adapter packages may log native settlement handoff, but such logs
must not imply Bondstone owns broker retry, dead-letter, topology, or
monitoring policy.

For operational procedures and ownership boundaries, see
[operations.md](operations.md).
