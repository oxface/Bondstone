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

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:test:integration` for provider behavior.
