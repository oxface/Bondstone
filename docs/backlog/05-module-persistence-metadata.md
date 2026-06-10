# Module Persistence Metadata

Goal: decide whether module persistence metadata should stay on general module
registration or move into provider-owned module capability metadata.

## Scope

- Review `PersistenceProviderName`, `PersistenceContextType`,
  `UsePersistence(...)`, provider-specific module persistence helpers, and
  module-owned persistence resolvers.
- Record the current fallback finding: fallback non-module persistence
  services are functional through root-level provider registration when no
  module-owned implementations exist, but current preferred module-boundary
  setup does not use them.
- Separate durable module capability metadata from provider implementation
  details such as EF DbContext type or PostgreSQL schema/session configuration.
- Evaluate a shared module capability registry keyed by module name with
  segregated resolver interfaces.
- Decide what happens to fallback non-module persistence services.

## ADRs

- [0042 Module Persistence Capability Metadata](../adr/0042-module-persistence-capability-metadata.md)
- [0046 Public API Surface Policy](../adr/0046-public-api-surface-policy.md)

## Review Questions

- Should `PersistenceContextType` remain public on general module registration?
- Should `PersistenceProviderName` become provider-owned metadata rather than a
  string on module registration?
- Are fallback `IDurableOutboxWriter`, `IDurableInboxHandlerExecutor`, and
  operation-state services intentionally supported advanced composition, or
  test-only compatibility that should be retired?
- Can common resolver diagnostics be centralized without creating a service
  locator surface?

## Candidate Deliverables

- Accepted, rejected, or narrowed ADR 0042.
- Stable module/persistence docs updated with the accepted metadata model.
- If fallback services remain, explicit docs that they are advanced
  single-store composition and not the preferred module-owned path.
- If fallback services are retired, compatibility/migration notes and focused
  implementation tasks.

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:pack`
