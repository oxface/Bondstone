---
baseline_commit: 9dd9da7
---

# Story 5.1: Product Boundary And Extraction Guardrail

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want runtime implementation stories to preserve Bondstone's library boundary,
so that v2 work does not drift into a bus, workflow engine, code generator, or application framework.

## Acceptance Criteria

1. Given a runtime story changes messaging, hosting, or package APIs, when it is reviewed, then it names how the change preserves durable module boundaries rather than generic bus or workflow scope.
2. Given a module moves from in-process composition toward service extraction, when contracts are reused, then stable message identities, handler patterns, inbox/outbox semantics, and operation observation remain valid.
3. Given a microservice host uses Bondstone, when broker infrastructure is configured, then durability remains module-owned and transport topology remains host-owned.
4. Given docs describe product scope, when they mention unsupported categories, then they keep bus, workflow, generator, SaaS, and app-platform scope out of Bondstone's current promise.

## Tasks / Subtasks

- [x] Inventory current product-boundary language before editing. (AC: 1, 2, 3, 4)
  - [x] Review `README.md`, `AGENTS.md`, `_bmad-output/project-context.md`, and `_bmad-output/planning-artifacts/architecture.md` for the current library/framework boundary.
  - [x] Review `docs/setup.md`, `docs/package-discovery.md`, `docs/packaging.md`, `docs/operations.md`, and `docs/samples.md` for broker ownership, service extraction, operation observation, and local transport framing.
  - [x] Review transport package READMEs under `src/Bondstone.Transport.*` for thin-adapter and local/dev/test wording.
- [x] Add or tighten guardrail wording only where current docs leave runtime drift ambiguous. (AC: 1, 3, 4)
  - [x] Keep Bondstone framed as a durable module-boundary library, not a generic bus, workflow/process-manager framework, code generator, SaaS framework, or application platform.
  - [x] Keep broker topology, provisioning, credentials, native retry, dead-letter policy, prefetch/concurrency, monitoring, and deployment topology explicitly host-owned.
  - [x] Keep `Bondstone.Transport.Local` described as explicit local/dev/test infrastructure, not production broker durability or a hidden fallback.
  - [x] Keep Rebus and other bus/runtime integration app-owned around Bondstone envelope, dispatcher, serializer, and durable inbox boundaries.
- [x] Preserve service-extraction continuity requirements. (AC: 2)
  - [x] Ensure any touched docs retain the modular-monolith-first path and the ability to extract modules without replacing message contracts, handler patterns, stable identities, inbox/outbox semantics, or operation observation.
  - [x] Ensure operation result wording preserves accepted-work metadata plus operation observation, not durable send as RPC.
  - [x] Ensure samples remain adoption proofs, not product applications or sources of product-domain behavior for package code.
- [x] Add lightweight verification or tests only where a code/API change creates boundary risk. (AC: 1, 2, 3)
  - [x] For documentation-only edits, use targeted reference/wording sweeps and Prettier.
  - [x] If runtime code or public API changes are needed, add focused `Unit` or `Application` tests for the affected boundary and run public API baselines when packable package surfaces change.
  - [x] Use `Integration` tests only for provider-backed persistence, broker settlement, inbox/outbox, retry, or extraction behavior that cannot be proven in fast tests.
- [x] Verify the story outcome. (AC: 1, 2, 3, 4)
  - [x] Run targeted sweeps for unsupported scope expansion and host-owned broker language.
  - [x] Run Prettier on touched markdown and this story file.
  - [x] Run `pnpm check` if code, package metadata, public API baselines, or broad docs are changed.

### Review Findings

- [x] [Review][Patch] Include persistence changes in runtime boundary review triggers [_bmad-output/planning-artifacts/architecture.md:438]
- [x] [Review][Patch] Name handler-pattern continuity in service-extraction guardrails [_bmad-output/planning-artifacts/architecture.md:440]
- [x] [Review][Patch] Align repository review guidance with architecture's mandatory wording [docs/repository.md:61]

## Dev Notes

Story 5.1 starts the runtime guardrail epics. It may be satisfied by a narrow documentation/test review if the current tree already preserves the product boundary. Do not create runtime abstractions just to satisfy this story. The implementation should make future runtime stories harder to misread, not expand Bondstone's feature surface.

### Current State Intelligence

The authoritative product boundary is already present in BMAD architecture and project context:

- Bondstone is a .NET library for durable module boundaries that supports modular monoliths first and preserves a path to service extraction when a module needs independent scalability, deployment, or operational isolation.
- Bondstone is not a general-purpose message bus, workflow engine, saga/process-manager framework, broker topology manager, code generator, or SaaS application framework.
- The durable value proposition is stable module contracts, stable message identities, outbox/inbox behavior, handler patterns, and operation-observation semantics that survive a move from in-process composition to separate services.
- Microservice use is supported only where consumers still need module-owned durability and host-owned transport infrastructure.

Current consumer docs already contain important guardrails:

- `docs/setup.md` says real broker hosts own topology, subscriptions, native consumers, acknowledgement, retry, and dead-letter policy; Bondstone bridges with `IDurableEnvelopeDispatcher`, `IDurableMessageEnvelopeSerializer`, and durable inbox ingestion.
- `docs/setup.md` keeps durable send separate from RPC: durable send returns accepted-work metadata and operation results are observed through operation APIs.
- `docs/package-discovery.md` says RabbitMQ and Service Bus packages are thin native-driver envelope adapters and do not own topology, provisioning, subscription storage, retry, dead-letter policy, prefetch/concurrency, or monitoring.
- `docs/operations.md` frames Bondstone as owning durable module state and neutral envelope handling while the application owns migrations, broker infrastructure, replay/reset/purge/archive, stale-inbox recovery, broker message movement, and compensating business actions.
- `docs/samples.md` says samples are not product applications, must not drive product behavior into library packages, and should prove eventual service extraction without owning product UI, deployment, authentication, or broker topology.
- `src/Bondstone.Transport.Local/README.md` says local transport is explicit sample/test/local development infrastructure and not production broker durability, topology management, retry, dead-letter handling, or durable incoming inbox behavior.
- `src/Bondstone.Transport.RabbitMq/README.md` and `src/Bondstone.Transport.ServiceBus/README.md` both present the packages as thin native-driver adapter packages for hosts that already own broker topology.

Potential implementation may therefore be a targeted guardrail pass: tighten ambiguous wording, add a checklist-like review note, or add tests only if an actual code/API drift is discovered.

### Architecture Compliance

Follow these non-negotiable architecture rules:

- Keep module boundaries explicit. Cross-module restart-safe state changes use durable commands or integration events.
- Do not collapse commands, integration events, and domain events into one generic message abstraction.
- Do not make `IModuleCommandExecutor` a generic mediator.
- Do not make durable send return target handler results directly; operation APIs observe status and result.
- Do not make transport adapters own broker topology, provisioning, retries, dead-letter policy, credentials, prefetch/concurrency, or monitoring.
- Do not make local transport a production fallback for missing broker configuration.
- Do not add automatic domain-event dispatch or automatic domain-to-integration-event publication.
- Do not add provider-neutral broker runtime ownership, cleanup/retention workers, or saga/process-manager behavior without future BMAD PRD and architecture approval.

If implementation discovers a legitimate need to expand product scope, stop and update BMAD PRD and architecture first instead of slipping the expansion into docs, APIs, samples, or tests.

### File Structure Requirements

Likely documentation targets if wording needs tightening:

- `README.md`
- `AGENTS.md`
- `_bmad-output/project-context.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/setup.md`
- `docs/package-discovery.md`
- `docs/packaging.md`
- `docs/operations.md`
- `docs/samples.md`
- `src/README.md`
- `src/Bondstone.Transport.Local/README.md`
- `src/Bondstone.Transport.RabbitMq/README.md`
- `src/Bondstone.Transport.ServiceBus/README.md`

Likely test targets only if runtime or public API behavior changes:

- `tests/Bondstone.Tests` for core module command/messaging semantics.
- `tests/Bondstone.Composition.Tests` for setup/composition guardrails.
- `tests/Bondstone.Transport.Local.Tests` for local transport opt-in and non-production fallback semantics.
- `tests/Bondstone.Transport.RabbitMq.Tests` and `tests/Bondstone.Transport.ServiceBus.Tests` if adapter behavior changes.
- `tests/Bondstone.Samples.Tests` if service-extraction proof behavior changes.
- `tests/Bondstone.PublicApi.Tests` if packable public/protected APIs change.

Do not edit unrelated package docs, generated artifacts, GitHub Issues/Projects, or future epic implementation slices unless the boundary review proves they are directly implicated.

### Testing Requirements

Recommended documentation-only verification:

- `rg -n "general-purpose message bus|workflow engine|process-manager|code generator|SaaS|application framework|app platform|broker topology|dead-letter|topology management|hidden fallback|production broker fallback|service extraction|operation observation" README.md AGENTS.md docs src _bmad-output/planning-artifacts/architecture.md _bmad-output/project-context.md`
- `rg -n "Bondstone owns|application owns|host owns|host-owned|app-owned|module-owned|thin .*adapter|local/dev/test|not production" docs src README.md _bmad-output/project-context.md`
- `pnpm exec prettier --check <touched markdown files> _bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md _bmad-output/implementation-artifacts/sprint-status.yaml`

Runtime verification if code changes occur:

- Use `pnpm backend:test` for fast `Unit` and `Application` coverage.
- Use `pnpm backend:test:integration` for PostgreSQL, RabbitMQ, Service Bus, broker settlement, durable inbox/outbox, retry, or sample extraction behavior.
- Use `pnpm backend:pack` and public API baseline review when packable package API surface changes.
- Use `pnpm check` as the final gate for broad runtime, package, or cross-doc changes.

### Previous Story Intelligence

Story 5.1 is the first story in Epic 5, so there is no previous same-epic story. Recent documentation stories established patterns that still apply:

- Story 4.3 treated a guidance story as verification-first and only tightened wording where needed.
- Story 4.2 and Story 4.1 kept scoped reference work narrow, avoided runtime/package churn, and used targeted stale-reference sweeps before broader verification.
- Documentation-only completion is acceptable when the current repository already satisfies the acceptance criteria and the evidence is recorded.
- Keep durable architecture in BMAD artifacts and keep consumer/repository docs focused on usage, operations, package discovery, and workflow guidance.

### Git Intelligence

Recent commits:

- `9dd9da7 docs: readme and agents doc refine`
- `9bc4667 docs: metadata fix`
- `bb2c102 docs: bmad native docs`
- `783b260 docs: more bmad documents refactoring`
- `13140c5 docs: bmad project context`

The recent direction is BMAD-native source-of-truth routing with small, reviewable documentation passes. Preserve that pattern. Avoid broad rewrites or runtime expansion unless the boundary inventory exposes a concrete defect.

### Latest Technical Information

No external web research is required for this story. The story is governed by local BMAD product scope and repository documentation, not by a latest external API or framework version. If implementation changes RabbitMQ, Azure Service Bus, EF Core, PostgreSQL, .NET, or package public APIs, use the versions centralized in project context and package files, then consult official vendor documentation before changing behavior.

### Project Context Reference

Project context says Bondstone is a library/framework, not a product app, UI app, SaaS platform, workflow engine, or general-purpose bus. It also says local transport is explicit local/dev/test infrastructure only, transport adapters are thin native-driver envelope adapters, broker topology and operations remain host-owned, public API changes are compatibility-sensitive, and runtime package collaboration must use explicit contracts or package-local implementation instead of production `InternalsVisibleTo`.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3, FR4, FR5, FR7, FR8, FR9, FR10, non-goals, and UX not applicable
- `_bmad-output/planning-artifacts/architecture.md` - Product Positioning, Package Architecture, Module Command Pipeline, Durable Commands, Integration Events, Domain Events, Transport Boundary, Operation Observation, Documentation Ownership, Verification Strategy, Explicit Deferred Work
- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.1 acceptance criteria
- `_bmad-output/project-context.md` - critical runtime, code, testing, and workflow guardrails
- `README.md` - public repository positioning and BMAD routing
- `docs/setup.md` - host composition, broker ownership, durable send, and operation observation
- `docs/package-discovery.md` - package capability matrix and broker integration boundaries
- `docs/packaging.md` - dependency direction, package policy, and app-owned broker integration
- `docs/operations.md` - production ownership, receive semantics, broker settlement, and operation results
- `docs/samples.md` - sample and service-extraction guardrails
- `docs/testing.md` - test categories and verification surface
- `docs/public-api.md` - public API compatibility and thin adapter decision notes
- `src/Bondstone.Transport.Local/README.md` - local transport scope
- `src/Bondstone.Transport.RabbitMq/README.md` - RabbitMQ adapter scope
- `src/Bondstone.Transport.ServiceBus/README.md` - Service Bus adapter scope
- `_bmad-output/implementation-artifacts/4-3-update-github-workflow-guidance.md` - previous documentation-story pattern

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Resolved `bmad-dev-story` customization manually after `resolve_customization.py` failed because Python could not import `json`.
- Loaded project context, sprint status, Story 5.1, BMAD epic/architecture context, docs/testing.md, scoped docs/source/transport AGENTS files, consumer docs, and transport package READMEs.
- Ran targeted boundary sweeps for unsupported scope expansion, host-owned broker language, service extraction, operation observation, local transport fallback wording, and app-owned broker integration.
- Ran `pnpm exec prettier --check _bmad-output/planning-artifacts/architecture.md docs/repository.md _bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md _bmad-output/implementation-artifacts/sprint-status.yaml`.
- Ran `pnpm check`.

### Completion Notes List

- Added runtime implementation review guardrail wording to BMAD architecture so messaging, hosting, transport, and package API reviews must name durable module-boundary preservation, service-extraction continuity, module-owned durability, and host-owned broker topology.
- Added repository workflow guidance requiring runtime stories to record product-boundary review evidence and avoid describing Bondstone as a generic bus, workflow engine, code generator, SaaS framework, application platform, or broker runtime.
- Addressed code-review findings by including persistence, handler patterns, and mandatory repository review-evidence wording in the runtime product-boundary guardrails.
- No runtime code, public API, package metadata, or tests were changed; verification was documentation-focused plus the full repository quality gate.

### File List

- `_bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/repository.md`

## Change Log

- 2026-06-18: Added runtime product-boundary review guardrails to architecture and repository workflow docs; completed documentation-only verification for Story 5.1.
- 2026-06-18: Addressed code-review findings and marked Story 5.1 done.
