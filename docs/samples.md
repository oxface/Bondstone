# Samples

This document records the current Bondstone sample direction.

## Purpose

Samples exist to test and demonstrate Bondstone usage. They should apply
pressure across composition, persistence, transport, package references, local
tooling, and end-to-end workflows.

Samples are not product applications and must not drive product behavior into
library packages.

## First Sample Shape

Start and maintain an early sample application once enough package scaffold
exists to make it useful.

The first sample should be intentionally small and operationally boring:

- no authentication;
- no product-grade UI;
- if a UI exists, use bare controls such as buttons and simple status displays;
- no product-specific domain depth beyond what is needed to exercise
  Bondstone behavior;
- no deployment story beyond local verification unless a later ADR accepts it.

The sample should demonstrate:

- modular monolith composition;
- module-owned persistence;
- durable message identity and registration;
- inbox/outbox behavior;
- provider and transport adapter integration;
- eventual service extraction shape.

## Local Orchestration

Use Aspire as the preferred local orchestration host for samples.

The sample AppHost should start local dependencies and processes needed to
exercise Bondstone behavior, such as:

- databases;
- transport infrastructure;
- cache infrastructure when a sample needs it;
- backend hosts or workers;
- migrators;
- optional frontend processes.

Aspire is a local sample orchestration choice. It is not a deployment platform
decision for Bondstone packages.

## Current Status

The current `samples/ModularMonolith` project is a Phase 4 adoption-proof
minimal API sample. It is still intentionally small, but its app entrypoint
uses normal ASP.NET Core, Rebus service-provider registration, Bondstone
module registration, and the durable outbox worker. Verification-only database
reset and completion polling live in the integration test, not in the app
entrypoint.

The sample has `ordering` and `fulfillment` modules split into module-owned
assemblies with separate module-owned `DbContext` types and PostgreSQL
schemas. Each implementation assembly exposes a module-owned
`IBondstoneModule` registration object plus a thin host extension for the
connection string. The API host composes those module extensions, while the
module assemblies own durable messaging capability, PostgreSQL persistence
binding, and `RegisterFromAssemblyContaining<TMarker>()` handler scanning. The
sample sends a durable command from ordering to fulfillment, dispatches the
ordering outbox through the durable outbox worker and Rebus in-memory
transport, receives through the Rebus topology-bound module command endpoint
handler, and persists fulfillment state, inbox markers, and operation
completion through fulfillment EF persistence.

The focused smoke test lives in
[`tests/Bondstone.Samples.Tests`](../tests/Bondstone.Samples.Tests) and is an
`Integration` test because it uses Testcontainers PostgreSQL. Default fast
verification remains `Unit` and `Application` only; run sample smoke coverage
with the repository integration test entrypoint.

Once the MVP surface settles, polish or replace this sample so it demonstrates
the final preferred public API and application structure.

Phase 5 event samples should remain narrow until subscriber execution is
implemented. The first event sample update should prove explicit integration
event publication and Rebus topic dispatch only if it stays small; broader
event choreography and automatic domain-event publication remain out of the
sample until later ADR-backed slices accept them.
