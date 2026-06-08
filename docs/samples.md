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
harness, not the final user-facing sample. It deliberately stays close to the
implemented command-loop seams so it can validate real library behavior while
the MVP API is still settling.

The harness has `ordering` and `fulfillment` modules with separate
module-owned `DbContext` types and PostgreSQL schemas, sends a durable command
from ordering to fulfillment, dispatches the ordering outbox through Rebus
in-memory transport, receives through the Rebus module command endpoint
handler, and persists fulfillment state, inbox markers, and operation
completion through fulfillment EF persistence.

The focused smoke test lives in
[`tests/Bondstone.Samples.Tests`](../tests/Bondstone.Samples.Tests) and is an
`Integration` test because it uses Testcontainers PostgreSQL. Default fast
verification remains `Unit` and `Application` only; run sample smoke coverage
with the repository integration test entrypoint.

Once the MVP surface settles, replace this harness with a consumer-style sample
that demonstrates the preferred public API and application structure rather
than carrying verification-specific bootstrap code.
