# Transport And Hosting Ergonomics

Priority: Medium

Goal: improve operational ergonomics around workers, receive helpers, and
provider setup without turning Bondstone into a generic broker administration
layer.

## Scope

- Module-targeted outbox worker registration for selected-module dispatch or
  stronger noisy-neighbor isolation.
- Lower-level provider-native receive services used by both hosted workers and
  app-owned consumers.
- RabbitMQ or Service Bus topology declaration helpers as explicit opt-in
  development/deployment aids.
- Public multi-transport diagnostic report shapes if startup exception
  messages stop being enough.
- Broader provider-backed integration coverage when new adapter behavior needs
  it.

## Related ADRs

- [0038 Provider Retry Recovery And Settlement Boundaries](../adr/0038-provider-retry-recovery-and-settlement-boundaries.md)
- [0039 Startup Transport Topology Validation](../adr/0039-startup-transport-topology-validation.md)
- [0044 Module Outbox Worker Topology](../adr/0044-module-outbox-worker-topology.md)
- [0047 Explicit Deploy Provisioning Helpers](../adr/0047-explicit-deploy-provisioning-helpers.md)

## Intake From Item 08

RabbitMQ and Service Bus already expose lower-level receive dispatchers for
app-owned consumers/processors and hosted workers. Their settlement handler
helpers remain useful as provider-native acknowledgement/completion ordering
conveniences. A new receive service abstraction is deferred until custom loops
show that the dispatcher/handler split is actively awkward.

## Candidate Work Items

- Design selected-module outbox worker API only after accepting worker topology
  semantics.
- Extract provider-native receive processing services only if custom receive
  loops need cleaner composition than settlement delegates.
- Review receive helper names and docs after the public API inventory so
  dispatchers, mappers, settlement helpers, and hosted workers are clearly
  classified without breaking source compatibility.
- Keep provisioning helpers explicit; never run broker provisioning from
  normal `AddBondstone` transport registration.

## Proposed Slices

1. Receive-helper cleanup slice: from item 08, keep the dispatcher/settlement
   helper split unless custom receive loops show it is actively awkward; then
   clarify names/docs or extract a lower-level receive service with
   compatibility review.
2. Worker isolation slice: design selected-module outbox worker registration
   only if aggregate workers block real deployments.
3. Provisioning slice: prototype explicit topology declaration helpers without
   wiring them into normal application startup.
4. Diagnostics slice: add structured startup diagnostics only if exception
   messages are not enough for multi-transport failures.

## Implementation Backlog

### THE-01: Receive Helper Classification And Docs

Priority: P0 after API-01.

Classify RabbitMQ and Service Bus mappers, dispatchers, settlement helpers,
and workers as normal API or advanced composition API. Update transport docs
so app-owned consumer examples point to the intended low-level types.

Important files:

- `docs/architecture/transport-rabbitmq.md`
- `docs/architecture/transport-servicebus.md`
- `src/Bondstone.Transport.RabbitMq/Inbox`
- `src/Bondstone.Transport.ServiceBus/Inbox`
- `tests/Bondstone.Transport.RabbitMq.Tests`
- `tests/Bondstone.Transport.ServiceBus.Tests`

Verification:

- `pnpm format:check`
- `pnpm backend:test:fast`

### THE-02: Receive Helper Naming Cleanup

Priority: P1.

If THE-01 shows current names are actively confusing, introduce additive names
or docs aliases before any breaking rename. Keep source compatibility unless
ADR 0046 is amended with a compatibility plan.

Candidate types:

- `IRabbitMqReceivedMessageHandler`
- `RabbitMqReceivedMessageDispatcher`
- `IServiceBusReceivedMessageHandler`
- `ServiceBusReceivedMessageDispatcher`
- `IModuleCommandReceivePipeline`
- `IModuleEventReceivePipeline`

Verification:

- `pnpm backend:build`
- `pnpm backend:test:fast`
- `pnpm backend:pack`

### THE-03: Selected-Module Outbox Worker

Priority: P1 when deployment isolation is needed.

Design and implement opt-in module filtering for hosted outbox dispatch. Keep
the aggregate worker as the default, but allow selected modules to be run by
separate workers when noisy-neighbor isolation matters.

Important files:

- `src/Bondstone.Hosting`
- `src/Bondstone/Persistence/Resolution/DurableModuleOutboxDispatchAggregator.cs`
- `tests/Bondstone.Hosting.Tests`
- `tests/Bondstone.Tests/Persistence/DurableModuleOutboxDispatchAggregatorTests.cs`

Verification:

- `pnpm backend:build`
- `pnpm backend:test:fast`

### THE-04: Explicit Topology Provisioning Helpers

Priority: P2.

Prototype opt-in broker topology declaration helpers for development or
deployment tooling. Do not run these helpers from normal transport
registration or hosted worker startup.

Important files:

- `docs/adr/0047-explicit-deploy-provisioning-helpers.md`
- `src/Bondstone.Transport.RabbitMq`
- `src/Bondstone.Transport.ServiceBus`
- provider transport tests

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`

### THE-05: Worker Diagnostics And Timeouts

Priority: P2.

Add structured diagnostics, timeout options, or clearer logs only for concrete
failure modes found in provider-backed testing or real-project validation.

Candidate files:

- `src/Bondstone.Hosting`
- `src/Bondstone.Transport.RabbitMq/Inbox`
- `src/Bondstone.Transport.ServiceBus/Inbox`
- transport worker tests

Verification:

- `pnpm backend:test:fast`
- `pnpm backend:test:integration`

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` for provider behavior.
