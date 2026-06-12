# Public API And Composition Cleanup

Goal: classify and tighten Bondstone's public setup and advanced composition
surface before treating the current packages as compatibility-bearing stable
APIs.

## Why Now

Bondstone has accumulated public types from early extraction, tests, provider
experiments, and advanced composition needs. ADR 0046 accepts a
compatibility-first public API policy, but its application is still partial.

The module runtime isolation slice made one provider-facing surface sharper:
module-owned command and receive persistence now uses passive durable module
runtime registrations stored in `DurableModulePersistenceRegistrationRegistry`,
while executable writer, inbox executor, and operation-state services use
ordinary role contracts. That change is a useful pattern, but it also makes
the remaining public surface worth reviewing before real project readiness
work.

## Direction To Explore

- Inventory public types in `Bondstone`, EF Core, EF PostgreSQL, non-EF
  PostgreSQL, hosting, and transport packages.
- Classify each public type as normal setup API, user application contract,
  advanced composition API, provider/runtime contract, public implementation
  detail exposed for now, or candidate for removal before the first stable
  compatibility-bearing release.
- Prefer documentation and naming clarification over hiding types unless a
  type is clearly misleading or obsolete.
- Keep provider setup helpers as the normal path for applications.
- Keep advanced composition APIs available when tests, custom schedulers,
  provider adapters, or app-owned receive/dispatch loops need them.
- Do not introduce a public API baseline tool in this slice unless the
  inventory shows it is necessary for the next decision.

## Questions

- Which public concrete implementation types should remain public advanced
  composition APIs, and which should become internal before stable release?
- Should provider-facing runtime registration types live in a documented
  advanced composition section, or remain discoverable only through provider
  setup docs?
- Which package README files need quick-path cleanup so normal users do not
  start from advanced contracts?
- Is an automated public API baseline needed now, or should it remain a later
  package-compatibility task?

## Deliverables

- Public API inventory notes or a focused table for the packages touched.
- ADR amendment or new ADR if compatibility policy, package boundaries,
  public type visibility, or provider contracts change.
- Stable docs/README updates that clearly separate normal setup from advanced
  composition.
- Small code cleanup only where the inventory finds an obviously obsolete or
  misleading public surface and ADR 0046 compatibility expectations are met.

## Progress

- First-pass stable inventory for `Bondstone`, `Bondstone.Persistence`,
  `Bondstone.Persistence.EntityFrameworkCore`,
  `Bondstone.Persistence.EntityFrameworkCore.Postgres`, and
  `Bondstone.Persistence.Postgres` now lives in
  [../public-api.md](../public-api.md).
- Remaining inventory work should classify hosting, transport, and optional
  capability packages before proposing visibility, naming, or contract changes.

## Verification

- `pnpm format:check`
- `pnpm backend:build`
- `pnpm backend:test:fast`
