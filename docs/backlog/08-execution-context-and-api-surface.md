# Execution Context And API Surface

Goal: decide how much of Bondstone's execution context, receive-helper, and
low-level implementation surface should be stable public API.

## Scope

- Review `ModuleExecutionContextAccessor`, command/event execution context
  pipeline behaviors, durable send/publish APIs, and HTTP/custom execution
  scenarios.
- Decide whether `AsyncLocal` remains the only ergonomic source-module
  mechanism or whether explicit source-module APIs/module-scoped clients are
  needed.
- Review RabbitMQ and Service Bus receive settlement helper shapes and whether
  hosted workers should delegate to provider-native lower-level receive
  services.
- Classify public implementation classes as supported extension points,
  advanced composition APIs, or accidental public surface.

## ADRs

- [0045 Module Execution Context Semantics](../adr/0045-module-execution-context-semantics.md)
- [0046 Public API Surface Policy](../adr/0046-public-api-surface-policy.md)

## Review Questions

- Is ambient `AsyncLocal` module context acceptable for all current
  send/publish usage?
- Should custom HTTP command execution use existing module command routes or a
  distinct non-durable command execution contract?
- Should provider receive handlers keep settlement delegates, or should
  provider packages expose a lower-level receive processing service used by
  both hosted workers and app-owned consumers?
- Which public concrete classes are intentionally stable?

## Candidate Deliverables

- Accepted, rejected, or narrowed ADRs 0045 and 0046.
- Stable docs updated with accepted execution-context and public API policy.
- Follow-up implementation tasks for explicit APIs, receive helper reshaping,
  or visibility/API baseline work.

## Verification

- `pnpm backend:test:fast`
- `pnpm backend:pack`
