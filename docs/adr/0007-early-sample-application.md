# 0007 Early Sample Application

Status: Accepted
Application: Partially Applied
Date: 2026-06-03

## Context

Bondstone should support modular monoliths, service extraction, and
microservice setups that need internal durability. Unit and package-level tests
can protect many behaviors, but a sample application can apply pressure across
composition, persistence, transport, package references, local tooling, and
end-to-end workflows.

Starting a sample too late risks discovering API and composition problems only
after the core packages feel settled. Starting a sample too early can also
become a distraction if it turns into product development, auth work, UI
polish, or deployment scope.

The repository needs a sample strategy that gives design feedback without
turning samples into the main product.

## Decision

Start and maintain an early sample application once enough package scaffold
exists to make it useful.

The first sample should be intentionally small and operationally boring:

- no authentication;
- no product-grade UI;
- if a UI exists, use bare controls such as buttons and simple status displays;
- no product-specific domain depth beyond what is needed to exercise
  Bondstone behavior;
- no deployment story beyond local verification unless a later ADR accepts it.

The sample should exist to test and demonstrate Bondstone usage, especially:

- modular monolith composition;
- module-owned persistence;
- durable message identity and registration;
- inbox/outbox behavior;
- provider and transport adapter integration;
- eventual service extraction shape.

Use Aspire as the preferred local orchestration host for samples. The sample
AppHost should start the local dependencies and processes needed to exercise
Bondstone behavior, such as:

- databases;
- transport infrastructure;
- cache infrastructure when a sample needs it;
- backend hosts or workers;
- migrators;
- optional frontend processes.

Aspire is a local sample orchestration choice. It is not a deployment platform
decision for Bondstone packages.

The sample should stay updated as extraction proceeds. If it becomes too costly
or starts driving product behavior into library packages, pause and revisit the
sample strategy with an ADR.

## Consequences

An early sample can reveal composition and API problems before package
boundaries harden.

The sample can become a useful end-to-end and smoke-test target.

The repository will need to keep sample complexity intentionally low so the
sample does not become a second application product.

If UI is added, minimal frontend tooling may become justified for the sample,
but frontend/browser tooling should remain sample-driven rather than baseline
library infrastructure.

Aspire adds sample-maintenance cost, but it keeps multi-process sample startup
explicit and repeatable.

## Application Notes

- Current contract: Samples should stay small, exercise Bondstone behavior, and
  use Aspire as the preferred local orchestration host when orchestration is
  needed.
- Stable docs: Current sample direction is described in
  [docs/samples.md](../samples.md), with related testing and architecture
  context in [docs/testing.md](../testing.md) and
  [docs/architecture.md](../architecture.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) points agents to sample
  guidance before adding or changing samples.
- Application evidence: Sample direction is documented.
- Pending or deferred: Sample projects and runnable smoke paths have not been
  created yet.

## Verification

Read back [docs/samples.md](../samples.md),
[docs/testing.md](../testing.md), [docs/architecture.md](../architecture.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). A runnable
sample smoke path remains pending.
