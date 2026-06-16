# 0016 Non-Throwing Operation Wait Ergonomics

Status: Accepted
Application: Partially Applied
Date: 2026-06-16

## Context

`IDurableOperationResultReader` currently exposes `GetResultAsync<TResult>()`
for one-shot reads and `WaitForResultAsync<TResult>()` for explicit
timeout-bounded polling until an operation reaches a terminal state.

The current wait API returns a terminal `DurableOperationResult<TResult>` when
the operation reaches `Completed`, `Failed`, or `Cancelled`. It throws
`TimeoutException` when the caller's timeout expires first. This is
compatible with the current operation-state model because timeout is caller
patience, not durable operation state. Writing `Failed` or `Cancelled` remains
an explicit application policy through `IDurableOperationFinalizer`.

The throwing wait API is convenient for tests, demos, short-lived internal
flows, and callers that prefer exception-based timeout control. It is less
ergonomic for HTTP APIs and polling endpoints, where consumers often prefer a
single result value that distinguishes "terminal operation state" from
"caller stopped waiting."

## Decision

Bondstone should keep the existing `WaitForResultAsync<TResult>()` behavior for
compatibility: it polls for terminal operation state and throws
`TimeoutException` when the timeout expires before a terminal state is
observed.

A future additive API should be considered for non-throwing wait ergonomics.
That API should not encode timeout as a durable operation status. Instead it
should return a wrapper that separates the caller's wait outcome from the
current durable operation result.

The likely shape is a result such as:

```csharp
public sealed record DurableOperationWaitResult<TResult>(
    bool CompletedWithinTimeout,
    DurableOperationResult<TResult> Result);
```

The exact name and properties remain open. The important boundary is that a
wait timeout is not `Failed`, `Cancelled`, or `Unknown` operation state unless
application policy explicitly writes such a state.

Stable docs should continue to recommend `GetResultAsync<TResult>()` for API
endpoints that read current state once. `WaitForResultAsync<TResult>()` should
be documented as a convenience for explicit timeout-bounded waits, not as
durable RPC.

## Consequences

The current public API remains compatible and honest. Existing callers that
expect `TimeoutException` keep working.

A future non-throwing wait API can improve endpoint ergonomics without
confusing operation state with caller waiting behavior.

The docs must be precise: `WaitForResultAsync<TResult>()` does not return a
timeout result today. It throws on timeout. Timeout does not finalize the
operation.

## Related Decisions

- Narrows [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md).
- Relates to [0014 Production Operations And Lifecycle Guidance](0014-production-operations-and-lifecycle-guidance.md).

## Application Notes

- Current contract: `WaitForResultAsync<TResult>()` throws
  `TimeoutException` if no terminal state is observed before the caller's
  timeout.
- Stable docs: messaging architecture and setup docs describe the throwing
  timeout behavior and recommend `GetResultAsync<TResult>()` for current-state
  endpoint reads. [operations.md](../operations.md) also documents timeout,
  finalization, and expiration ownership for production callers.
- Agent guidance: no new agent rule is required until a future API is
  accepted.
- Application evidence: tests cover terminal success, terminal failure,
  handle-based polling, and timeout throwing behavior; stable operations docs
  now state that wait timeout is caller patience and does not write durable
  operation state.
- Pending or deferred: design and add a non-throwing wait API only when
  endpoint ergonomics justify public API expansion.

## Verification

Accepted during v2 planning. Stable docs and interface XML comments were
corrected in the same work item, and focused unit coverage was added for
current timeout behavior. Application remains partial because the existing
throwing behavior is documented and tested, while the additive non-throwing
API remains deferred.

On 2026-06-16, added production operations guidance for result polling,
throwing wait timeout behavior, explicit finalization, and app-owned
expiration jobs.
