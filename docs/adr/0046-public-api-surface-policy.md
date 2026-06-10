# 0046 Public API Surface Policy

Status: Proposed
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

Decide a public API surface policy before tightening implementation
visibility, adding compatibility tests, or marking advanced APIs.

The candidate direction is:

- Keep clear user-facing contracts and builder APIs public.
- Mark low-level composition types as intentionally advanced where they remain
  public.
- Hide or stop expanding public implementation classes that are not intended
  as durable extension points.
- Add an API review or baseline process before making compatibility promises
  stronger.

## Consequences

A policy would reduce accidental compatibility pressure and help future agents
know whether a type is a stable extension point or an implementation detail.

Tightening public types after publication can be breaking and needs careful
versioning and release-note treatment.

Leaving the surface broad avoids immediate churn but makes future cleanup
harder.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)

## Application Notes

- Current contract: proposed only; no binding change yet.
- Stable docs: if accepted, update packaging and repository docs with API
  stability and advanced-composition guidance.
- Agent guidance: if accepted, update root AGENTS with compatibility and
  public surface rules.
- Application evidence: current packages build and pack with a broad public
  implementation surface.
- Pending or deferred: decide baseline tooling, breaking-change policy, and
  which public implementation classes remain supported.

## Verification

No executable verification yet; this is a proposed decision draft.
