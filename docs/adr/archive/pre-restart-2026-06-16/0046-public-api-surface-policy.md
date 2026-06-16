# 0046 Public API Surface Policy

Status: Archived
Application: Not Applicable
Date: 2026-06-10

## Context

Bondstone has already published initial packages. The current public surface
includes user-facing contracts, setup builders, result types, provider
topology types, and many concrete implementation classes that are public to
support advanced composition and tests.

This broad surface is useful during early extraction and stabilization, but it
can accidentally create compatibility expectations around implementation
types, low-level constructors, and internal composition patterns.

## Decision

Bondstone adopts a compatibility-first public API surface policy before any
cleanup removes, hides, or renames public types.

Clear user-facing contracts, attributes, result types, setup builders, module
registration APIs, and normal host composition extensions remain public stable
API.

Low-level persistence, transport, receive, dispatcher, resolver, diagnostic,
and provider composition contracts may also remain public when they are needed
for tests, app-owned consumers, custom schedulers, provider integration, or
advanced composition. These APIs should be documented as advanced when they are
not the normal setup path.

Public concrete implementation classes that exist because of early extraction
or advanced composition are not automatically open-ended extension points.
They should not grow additional public surface casually. Hiding, renaming, or
removing them after publication requires an explicit compatibility plan,
release-note treatment, and, for broad cleanup, follow-up ADR review.

Bondstone should add a public API inventory or baseline before making
compatibility promises stronger or performing broad surface reduction.

Amendment, 2026-06-16:

The post-MVP persistence inspection APIs use a deliberate public split:
application-facing inspectors remain normal user contracts, while provider
inspection stores and module inspection-store registrations remain
provider/runtime contracts. The registration types may stay public when they
are the explicit collaboration point between Bondstone runtime and provider
packages, but they should be hidden from normal IntelliSense and documented as
advanced/provider hooks.

Public implementation types should not be reduced one at a time only because
they are concrete. Reduction should remove an obsolete capability, package, or
misleading setup path, or introduce a clearer provider contract first.

## Consequences

The policy reduces accidental compatibility pressure and helps future agents
distinguish normal setup APIs, advanced composition APIs, and public
implementation details exposed for now.

Tightening public types after publication can be breaking and needs careful
versioning and release-note treatment.

The current broad surface remains in place until the cleanup backlog completes
an inventory and compatibility plan.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)
- [0045 Module Execution Context Semantics](0045-module-execution-context-semantics.md)

## Application Notes

- Current contract: normal setup APIs stay public, low-level public APIs are
  advanced composition unless documented otherwise, and broad public surface
  removal or renaming requires compatibility planning before implementation.
- Stable docs: applied to [docs/packaging.md](../packaging.md). Existing
  architecture docs already steer normal users to `AddBondstone`, module-owned
  persistence, provider transport builders, and documented receive helpers.
- Agent guidance: root [AGENTS.md](../../AGENTS.md) now records the
  compatibility-first public API rule.
- Application evidence: current packages still build with the broad public
  implementation surface. The package-by-package classification inventory is
  recorded in [docs/public-api.md](../public-api.md). The automated baseline in
  `tests/Bondstone.PublicApi.Tests` compares the public/protected surface of
  every packable package against checked-in baselines during the fast test
  gate. `Bondstone.Utility.StringExtensions` has been internalized as
  package-local implementation code and removed from the
  `Bondstone.Persistence` public API baseline.
  `BondstoneLocalServiceCollectionExtensions` has been internalized as local
  transport registration plumbing and removed from the
  `Bondstone.Transport.Local` public API baseline.
- Pending or deferred: cleanup candidates and any public surface reduction are
  tracked in GitHub Issues and GitHub Projects. Those changes still require
  compatibility planning, ADR review when public API shape or package
  boundaries change, baseline diffs, and release-note treatment.
- 2026-06-16 curation: persistence inspection contracts are classified in
  [docs/public-api.md](../public-api.md). The inspection registrations remain
  public provider/runtime hooks and are hidden from normal IntelliSense.
  No additional concrete implementation types were removed during this
  curation pass.

## Verification

Read back this ADR and affected stable docs. The baseline application and
`StringExtensions` cleanup were verified with:

- `dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release`
- `pnpm check`
- `git diff --check`

The 2026-06-16 curation amendment was verified by reading the public API
inventory and running the default repository check gate.
