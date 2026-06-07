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

This sample direction is accepted and documented. Sample projects and runnable
smoke paths remain deferred; current implementation status is summarized in
[mvp-plan.md](mvp-plan.md).
