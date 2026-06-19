---
baseline_commit: 9dd9da7
---

# Story 5.2: Module Registration And Host Composition

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a host developer,
I want modules to own their registration metadata while hosts compose them,
so that module boundaries stay explicit and extraction-friendly.

## Acceptance Criteria

1. Given a module is registered, when durable messaging is enabled, then module name, persistence binding, command handlers, validators, published events, and subscriber handlers are declared by the module.
2. Given a host composes an application, when it calls `AddBondstone`, then it can compose module-owned extension methods with host-owned environment inputs.
3. Given duplicate or conflicting module durable registrations exist, when startup validation runs, then it fails with a stable diagnostic shape.
4. Given a module registers only remote durable message identities, when no local route exists, then registration does not imply a local handler.

## Tasks / Subtasks

- [x] Inventory current registration behavior before changing APIs. (AC: 1, 2, 3, 4)
  - [x] Review `IBondstoneModule`, `BondstoneModuleBuilder`, `BondstoneModuleBuilderExtensions`, `BondstoneBuilder`, `BondstoneServiceCollectionExtensions`, route/event/message registries, and durable persistence registration validators.
  - [x] Review sample module registration objects and host extensions in `samples/ModularMonolith.*/*ModuleRegistration.cs` and `*BondstoneModule.cs`.
  - [x] Record in completion notes how the change preserves module-owned metadata, host-owned environment inputs, and service-extraction continuity.
- [x] Preserve and tighten module-owned metadata registration. (AC: 1)
  - [x] Keep `IBondstoneModule.Configure(BondstoneModuleBuilder module)` as the preferred module-owned registration object.
  - [x] Ensure durable modules declare `UseDurableMessaging()`, persistence binding, command handlers, command validators, published events, and subscriber handlers inside module registration code or module-owned extension methods.
  - [x] Do not move module-owned durable metadata into host composition, sample app startup, transport adapters, or persistence providers except through provider-specific module helpers.
- [x] Preserve host composition through `AddBondstone`. (AC: 2)
  - [x] Keep hosts composing modules through `AddModule(...)`, module-owned host extensions such as `AddFulfillmentModule(connectionString)`, provider persistence helpers, explicit transport/dispatcher setup, and hosted workers.
  - [x] Keep connection strings, credentials, topology names, worker settings, broker retry/dead-letter policy, and local/real transport selection host-owned.
  - [x] Do not introduce a product app bootstrapper, service locator, module auto-discovery requirement, generic bus DSL, or hidden local transport fallback.
- [x] Validate duplicate and conflicting durable registrations with stable diagnostics. (AC: 3)
  - [x] Cover conflicts for module persistence provider/context, command routes, durable message type names, published events, event subscribers, and durable module persistence role registrations where applicable.
  - [x] Diagnostics must consistently include the normalized module name and the conflicting role or durable identity, and must be asserted by stable substrings rather than brittle full-message text.
  - [x] Do not invent a finalized stable setup-code vocabulary in this story; stable setup codes are deferred to later diagnostics work unless BMAD PRD/architecture is updated.
- [x] Preserve remote-contract-only identity registration. (AC: 4)
  - [x] Keep `BondstoneBuilder.RegisterMessage<TMessage>()`, `RegisterMessagesFromAssembly(...)`, and `RegisterMessagesFromAssemblyContaining<TMarker>()` as identity-only registration APIs.
  - [x] Ensure identity-only registration does not add command routes, event subscribers, handlers, module registrations, or local receive bindings.
  - [x] Cover a remote durable command identity and, if touched, assembly-scanned remote contract identities.
- [x] Add or update focused tests. (AC: 1, 2, 3, 4)
  - [x] Use `tests/Bondstone.Tests` for core module registration, registry, and diagnostic behavior.
  - [x] Use `tests/Bondstone.Composition.Tests` for cross-package `AddBondstone` composition and module-owned extension patterns.
  - [x] Use `Unit` tests for pure registry/validator behavior and `Application` tests for service-provider composition.
  - [x] Update public API baselines only if packable public/protected API changes are intentional and compatibility-reviewed.
- [x] Verify the story outcome.
  - [x] Run targeted tests first, for example `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` and `dotnet test tests/Bondstone.Composition.Tests/Bondstone.Composition.Tests.csproj --configuration Release --filter "Category=Application"`.
  - [x] Run `pnpm backend:test` for fast repository coverage after runtime changes.
  - [x] Run `pnpm backend:pack` and public API baseline checks if public/protected package surface changes.
  - [x] Run `pnpm check` for the final broad gate when code, public API, package metadata, samples, or broad docs change.

### Review Findings

- [x] [Review][Patch] Explicit null durable command identity falls back to metadata registration [src/Bondstone/Modules/Registration/BondstoneModuleCommandBuilder.cs:265] — fixed by splitting metadata and explicit-identity registration helpers and adding regression coverage for the named `messageTypeName: null!` overload path.

## Dev Notes

Story 5.2 is a runtime/API story. It should improve or verify the current module registration model without broadening Bondstone into a generic bus, workflow engine, code generator, application platform, broker runtime owner, or module auto-discovery framework.

### Current State Intelligence

The current tree already has the intended basic shape:

- `src/Bondstone/Modules/Contracts/IBondstoneModule.cs` defines a module-owned registration object with stable `Name` and `Configure(BondstoneModuleBuilder module)`.
- `src/Bondstone/Modules/Registration/BondstoneModuleBuilderExtensions.cs` supports host composition through `bondstone.Module(...)`, `bondstone.AddModule<TModule>()`, and `bondstone.AddModule(IBondstoneModule)`.
- `src/Bondstone/Modules/Registration/BondstoneModuleBuilder.cs` registers module metadata on creation and exposes `UseDurableMessaging()`, `UsePersistence(...)`, `Commands`, and `Events`.
- `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs` owns `AddBondstone(...)`, default singleton registries, core runtime service registration, configuration validators, and validation after the host configure callback.
- `src/Bondstone/Configuration/BondstoneBuilder.cs` owns message identity registration APIs and creates scoped module builders. Its `RegisterMessage<TMessage>()` and assembly variants register durable identities only.
- `src/Bondstone/Modules/Registration/BondstoneModuleRegistry.cs` normalizes module names and merges repeated module declarations. It throws when a module is configured with conflicting persistence provider or context.
- `src/Bondstone/Modules/Routing/ModuleCommandRouteRegistry.cs` prevents conflicting command routes by module plus command type and durable command routes by module plus message identity.
- `src/Bondstone/Messaging/Identity/MessageTypeRegistry.cs` prevents one CLR message type from being registered with conflicting identities and prevents one durable identity from mapping to multiple CLR types.
- `src/Bondstone/Modules/Events/ModulePublishedEventRegistry.cs` and `ModuleEventSubscriberRegistry.cs` prevent conflicting published-event and subscriber registrations.
- `src/Bondstone.Persistence/Persistence/Registration/DurableModulePersistenceRegistrationRegistry.cs` prevents duplicate module-specific persistence role registrations and uses normalized module names.
- `src/Bondstone/Configuration/DurableModulePersistenceConfigurationValidator.cs` catches durable module persistence roles registered for unknown modules and durable modules missing required module persistence roles at `AddBondstone` startup validation.

Existing tests already cover much of the target behavior:

- `tests/Bondstone.Tests/Modules/ModuleRegistrationTests.cs` covers module metadata, durable messaging metadata, persistence metadata, repeated module merge behavior, conflicting persistence provider/context, `AddModule`, and missing module lookup.
- `tests/Bondstone.Tests/Modules/ModuleCommandRegistrationTests.cs` covers handler and validator registration, explicit durable identities, duplicate command routes, assembly scanning, and `AddModule` registering module-owned command metadata.
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs` covers published event registration, subscriber metadata, explicit event identity, blank subscriber identity, and conflicting subscriber registrations.
- `tests/Bondstone.Tests/Configuration/BondstoneBuilderTests.cs` already proves `RegisterMessage<TMessage>()` registers a remote command identity without creating a route.
- `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs` covers duplicate durable module persistence roles, missing module role registrations, unknown module persistence registrations, and startup failures that prevent fallback root services from hiding module-runtime gaps.
- `tests/Bondstone.Composition.Tests/AddBondstoneCompositionTests.cs` covers resolvable outbox composition and receive-pipeline composition through `AddBondstone`.

The implementation may therefore be a focused validation/test/doc pass if the existing behavior already satisfies the acceptance criteria. Add runtime API only when a concrete gap remains after inventory.

### Architecture Compliance

Follow these non-negotiable rules:

- Modules own module name, durable messaging capability, persistence binding, command handlers, validators, published integration events, and subscriber handlers.
- Hosts compose modules through `AddBondstone` and module-owned extension methods while supplying host-owned environment inputs.
- Broker topology, provisioning, credentials, retry, dead-letter policy, prefetch/concurrency, native monitoring, and worker placement remain host-owned.
- `Bondstone.Transport.Local` remains explicit local/dev/test infrastructure, not a production fallback.
- Remote service extraction must keep stable message identities, stable handler/subscriber identities, handler patterns, inbox/outbox semantics, and operation observation intact.
- `RegisterMessage<TMessage>()` registers remote durable contract identity only. It must not create a local command route, subscriber binding, handler registration, module registration, or receive binding.
- Do not use `InternalsVisibleTo` for production package collaboration. Use explicit contracts or package-local implementation.
- Treat public/protected API changes as compatibility-sensitive. Inventory normal setup APIs and documented advanced composition APIs before adding, renaming, hiding, or removing members.

### File Structure Requirements

Likely UPDATE files if implementation changes are needed:

- `src/Bondstone/Configuration/BondstoneServiceCollectionExtensions.cs` - `AddBondstone` orchestration, default owned registry setup, validator registration, validation timing, and composition guardrails.
- `src/Bondstone/Configuration/BondstoneBuilder.cs` - builder surface, message identity-only registration, module builder creation, and validation context construction.
- `src/Bondstone/Configuration/BondstoneConfigurationValidationContext.cs` - data available to validators; extend only if diagnostics need existing registration metadata.
- `src/Bondstone/Configuration/DurableMessagingConfigurationValidator.cs` - startup checks for durable handlers/subscribers and durable messaging opt-in.
- `src/Bondstone/Configuration/DurableModulePersistenceConfigurationValidator.cs` - startup checks for module-specific persistence role completeness and unknown module registrations.
- `src/Bondstone/Modules/Contracts/IBondstoneModule.cs` and `IBondstoneModuleRegistry.cs` - public module contract; change only with compatibility review.
- `src/Bondstone/Modules/Registration/BondstoneModuleBuilder.cs` - module-owned metadata entrypoint; preserve `UseDurableMessaging`, `UsePersistence`, `Commands`, and `Events`.
- `src/Bondstone/Modules/Registration/BondstoneModuleBuilderExtensions.cs` - host-facing module composition helpers.
- `src/Bondstone/Modules/Registration/BondstoneModuleRegistry.cs` and `BondstoneModuleRegistration.cs` - module metadata storage and conflict checks.
- `src/Bondstone/Modules/Registration/BondstoneModuleCommandBuilder.cs` and `BondstoneModuleEventBuilder.cs` - module-owned handler, validator, published-event, and subscriber registration.
- `src/Bondstone/Modules/Routing/ModuleCommandRouteRegistry.cs`, `src/Bondstone/Modules/Events/ModulePublishedEventRegistry.cs`, and `src/Bondstone/Modules/Events/ModuleEventSubscriberRegistry.cs` - duplicate/conflict detection.
- `src/Bondstone/Messaging/Identity/MessageTypeRegistry.cs` - durable identity-only registration and duplicate identity checks.
- `src/Bondstone.Persistence/Persistence/Registration/DurableModulePersistenceRegistrationRegistry.cs` - module persistence role duplicate checks.

Likely test targets:

- `tests/Bondstone.Tests/Modules/ModuleRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleCommandRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs`
- `tests/Bondstone.Tests/Configuration/BondstoneBuilderTests.cs`
- `tests/Bondstone.Tests/Persistence/DurableModulePersistenceRegistrationTests.cs`
- `tests/Bondstone.Composition.Tests/AddBondstoneCompositionTests.cs`

Likely sample/doc targets only if examples need alignment:

- `docs/setup.md`
- `docs/package-discovery.md`
- `samples/README.md`
- `samples/ModularMonolith.Ordering/OrderingBondstoneModule.cs`
- `samples/ModularMonolith.Ordering/OrderingModuleRegistration.cs`
- `samples/ModularMonolith.Fulfillment/FulfillmentBondstoneModule.cs`
- `samples/ModularMonolith.Fulfillment/FulfillmentModuleRegistration.cs`
- `samples/ModularMonolith.Billing/BillingBondstoneModule.cs`
- `samples/ModularMonolith.Billing/BillingModuleRegistration.cs`

Do not add generated artifacts, packed packages, temporary sample outputs, broad docs rewrites, or new transport/runtime ownership to satisfy this story.

### Testing Requirements

Use the repository testing policy:

- `Unit` for registry, validation, duplicate/conflict diagnostics, identity-only registration, and public contract behavior with no external infrastructure.
- `Application` for `AddBondstone` composition that builds a service provider and crosses package boundaries without external IO.
- `Integration` only if a change depends on PostgreSQL, RabbitMQ, Azure Service Bus, durable inbox/outbox provider behavior, broker settlement, or sample smoke behavior.
- EF Core InMemory is not proof of relational durability, uniqueness, transactions, locking, claiming, retries, or PostgreSQL behavior.

Prefer assertions that prove observable behavior:

- registry metadata contents;
- absence of command routes/subscribers for identity-only remote contracts;
- startup validation throws before runtime fallback services hide incomplete module registration;
- diagnostics include module name, role/identity, and conflict class.

Avoid brittle tests that assert whole exception strings unless the implementation intentionally introduces a stable diagnostic object or code surface in an approved follow-up.

### Previous Story Intelligence

Story 5.1 completed the runtime product-boundary guardrail. Carry these learnings forward:

- Runtime stories must record review evidence for the product boundary they preserve.
- Review evidence should name durable module boundaries, service-extraction continuity, stable message identities, stable handler identities, handler patterns, inbox/outbox semantics, operation observation, module-owned durability, and host-owned broker topology.
- Do not describe runtime changes as making Bondstone a generic bus, workflow engine, code generator, SaaS framework, application platform, or broker runtime owner.
- Documentation-only or validation-first completion is acceptable when the current tree already satisfies the story and evidence is recorded.

Story 5.1 touched `_bmad-output/planning-artifacts/architecture.md`, `docs/repository.md`, `_bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md`, and `sprint-status.yaml`. Do not overwrite those local edits while implementing 5.2.

### Git Intelligence

Recent commits:

- `9dd9da7 docs: readme and agents doc refine`
- `9bc4667 docs: metadata fix`
- `bb2c102 docs: bmad native docs`
- `783b260 docs: more bmad documents refactoring`
- `13140c5 docs: bmad project context`

Recent repository direction favors narrow, reviewable work; BMAD-native source routing; and explicit product-boundary evidence. Keep this story similarly scoped.

### Latest Technical Information

External research does not require a dependency upgrade for this story. The repository already centralizes `net10.0`, .NET SDK `10.0.108`, and package versions in `global.json`, `Directory.Build.props`, and `Directory.Packages.props`.

Relevant current guidance:

- Microsoft DI documentation continues to use `IServiceCollection` registration during app startup followed by service-provider construction. Keep `AddBondstone` as service-registration composition and do not build an internal service provider during registration.
- Microsoft DI guidelines recommend small, explicit, testable services and avoiding direct construction that hides dependencies. Preserve Bondstone's explicit registries/builders and module-owned registrations rather than adding global discovery or hidden runtime behavior.
- EF Core 10 is an LTS release that requires .NET 10 and is supported until November 10, 2028. Keep EF/PostgreSQL behavior on the centrally pinned EF Core 10 package line unless a separate upgrade story changes package versions.

Sources:

- https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview
- https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines
- https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, not a product app, SaaS framework, workflow engine, or general-purpose bus. It also says module boundaries must stay explicit, durable message identities and handler/subscriber identities must be stable and explicit, transport adapters are thin native-driver envelope adapters, broker operations remain host-owned, and production packages must collaborate through explicit contracts or package-local implementation rather than production `InternalsVisibleTo`.

### Open Questions

None blocking. If implementation discovers that "stable diagnostic shape" needs a public structured diagnostic code surface, stop and route that through BMAD PRD/architecture because stable setup codes are sequenced later.

### Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.2 acceptance criteria
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR3.3, FR4.1, FR4.2, and related durable messaging requirements
- `_bmad-output/planning-artifacts/architecture.md` - Product Positioning, Package Architecture, Module Ownership, Durable Commands, Integration Events, Transport Boundary, Diagnostics And Observability, Public API And Compatibility, Verification Strategy
- `_bmad-output/project-context.md` - critical runtime, code, testing, and workflow guardrails
- `docs/repository.md` - runtime implementation review evidence and product-boundary workflow guidance
- `docs/setup.md` - host composition, module registration examples, remote contract registration, and broker ownership guidance
- `docs/testing.md` - test categories and verification surface
- `docs/packaging.md` - package IDs, dependency direction, and public API/package compatibility posture
- `samples/README.md` - modular monolith and service-split path
- `_bmad-output/implementation-artifacts/5-1-product-boundary-and-extraction-guardrail.md` - previous same-epic story learnings

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-18: Red test run for module command/event identity conflict diagnostics failed because diagnostics lacked the required module/identity role substrings.
- 2026-06-18: Targeted diagnostic tests passed after adding module-scoped diagnostic wrapping/detail.
- 2026-06-18: `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "Category=Unit"` passed.
- 2026-06-18: `dotnet test tests/Bondstone.Composition.Tests/Bondstone.Composition.Tests.csproj --configuration Release --filter "Category=Application"` passed.
- 2026-06-18: `pnpm backend:build` passed with 0 warnings and 0 errors.
- 2026-06-18: `pnpm backend:test` passed for fast Unit/Application coverage.
- 2026-06-18: `pnpm check` passed, including format check, restore, build, fast tests, pack, and package tests.
- 2026-06-19: Code review found and fixed explicit null durable command identity fallback; `dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --filter "FullyQualifiedName~ModuleCommandRegistrationTests"` passed.
- 2026-06-19: Review fix validation passed with `pnpm format:check`, `pnpm backend:test`, and `pnpm check`.

### Completion Notes List

- Inventory confirmed the existing registration model already preserves the preferred module-owned `IBondstoneModule.Configure(BondstoneModuleBuilder module)` object, module-scoped `UseDurableMessaging()`, persistence binding, command handlers, validators, published events, and subscriber handlers.
- Samples continue to keep host-owned environment inputs in module-owned host extensions (`AddOrderingModule(connectionString)`, `AddFulfillmentModule(connectionString)`, `AddBillingModule(connectionString)`) while module objects own durable metadata.
- Tightened conflict diagnostics for module-owned command and event message identity registration so durable command route and event identity conflicts include normalized module context plus the conflicting role or durable identity.
- Added guardrail tests for identity-only remote registration, assembly-scanned remote contract identities, module-owned host extension composition, and stable diagnostic substrings for module persistence, command route, published event, event subscriber, and durable module persistence conflict paths.
- Preserved service-extraction continuity: remote contracts remain identity-only via `RegisterMessage`/assembly registration, no local handler/route/subscriber/module binding is implied, and stable message/handler/subscriber identities remain explicit.
- No public/protected API surface changes were introduced; public API baselines were not updated.

### File List

- `_bmad-output/implementation-artifacts/5-2-module-registration-and-host-composition.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Bondstone/Modules/Registration/BondstoneModuleCommandBuilder.cs`
- `src/Bondstone/Modules/Registration/BondstoneModuleEventBuilder.cs`
- `src/Bondstone/Modules/Routing/ModuleCommandRouteRegistry.cs`
- `tests/Bondstone.Composition.Tests/AddBondstoneCompositionTests.cs`
- `tests/Bondstone.Tests/Configuration/BondstoneBuilderTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleCommandRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleEventRegistrationTests.cs`
- `tests/Bondstone.Tests/Modules/ModuleRegistrationTests.cs`

## Change Log

- 2026-06-18: Added module-scoped durable identity diagnostics and focused registration/composition guardrail tests.
