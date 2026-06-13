# 0022 Cancellation Token Parameter Names

Status: Accepted
Application: Applied
Date: 2026-06-05

## Context

Bondstone public APIs, implementations, and tests currently use mixed
`CancellationToken` parameter names, including `cancellationToken`,
`stoppingToken`, and `ct`. In C#, public parameter names are part of the
consumer-facing API because callers can use named arguments, so even a naming
cleanup affects public API shape.

The repository is still early in extraction, before a broader compatibility
policy has been accepted. Consistent naming now keeps signatures shorter and
reduces local style churn before more package surface is published and used.

## Decision

Use `ct` as the standard parameter and local variable name for
`CancellationToken` values in Bondstone C# source and tests.

This applies to public interfaces, public implementations, protected overrides,
private helpers, test doubles, and call-site named arguments. Existing
framework override semantics remain unchanged; only the local parameter name
changes.

Future Bondstone C# APIs should use `ct` for `CancellationToken` parameters
unless an external interface or generated code requires a different name.

## Consequences

Signatures become shorter and consistent across packages.

The change is source-visible for consumers that call current public methods
with named arguments such as `cancellationToken:`. That break is accepted while
Bondstone is still in early extraction and before a later compatibility ADR
sets stricter API-change rules.

The convention does not change cancellation behavior, default values, overload
ordering, or async execution semantics.

## Related Decisions

- [0005 Slow Layered Extraction](0005-slow-layered-extraction.md)
- [0006 Testing Strategy](0006-testing-strategy.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)

## Application Notes

- Current contract: Bondstone C# source and tests use `ct` for
  `CancellationToken` parameters and local variables.
- Stable docs: Current repository coding guidance is described in
  [docs/repository.md](../repository.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) directs agents to use
  `ct` for new `CancellationToken` parameters.
- Application evidence: Current source and test `CancellationToken` parameter
  names have been updated to `ct`.
- Pending or deferred: No pending application work.

## Verification

Read back the ADR, stable docs, and agent guidance. Ran the repository default
quality gate after applying the rename.
