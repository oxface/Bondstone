# Backlog

This folder is for ad hoc planning. It is not a maintained product roadmap,
priority queue, or current operating contract.

Stable docs describe the current repository state. ADRs preserve durable
decision history. Backlog files capture known pressure points and the one or
few active issue notes being explored now.

## How To Use

- Start with [00-plans.md](00-plans.md) for loose future-work context.
- Use active issue notes only when they match the current task.
- Extract a new issue note from `00-plans.md` when a topic becomes immediate.
- When an issue is resolved, move accepted decisions into ADRs, move current
  behavior into stable docs, and return remaining ideas to `00-plans.md`.
- Delete resolved issue notes unless they still contain short-term context
  that has not moved elsewhere.

Do not keep old issue notes up to date as a shadow backlog. The maintenance
cost belongs in ADRs and stable docs, not in speculative planning files.

## Active Issue Notes

- [01-public-api-and-composition-cleanup.md](01-public-api-and-composition-cleanup.md)
  explores public API classification and setup/composition cleanup before
  stable compatibility expectations harden.

## Historical Notes

[archive/](archive/) preserves old planning campaigns for traceability only.
Archived backlog files are not maintained and must not steer new
implementation.
