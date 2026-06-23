# GitHub Work Tracking

This document records the current Bondstone GitHub Issues and Projects
workflow. The SpecKit constitution, durable docs, and feature artifacts
describe durable requirements, architecture, governance, and implementation
deltas. GitHub Issues and GitHub Projects track backlog work, real-project
findings, cleanup tasks, prioritization, and ownership.

## Project Status

Use the Bondstone GitHub Project as the active work board.

- `Inbox`: captured but not yet prioritized.
- `Ready`: selected and clear enough for an agent to start.
- `In Progress`: actively being worked.
- `Blocked`: cannot make progress without external input or state.
- `Done`: completed and verified.

Move an item to `In Progress` before starting implementation work. Move it to
`Done` only after the issue has an implementation or trial-result comment,
verification is recorded, and the issue is closed when appropriate.

## Labels

Use existing labels before creating new ones.

Common label families:

- General type: `bug`, `enhancement`, `documentation`.
- Bondstone type: `type:cleanup`, `type:trial`.
- Area: `area:api`, `area:transport`, `area:template`.
- Decision signal: `architecture-review-required`.

Use `architecture-review-required` when the issue affects public API, package
boundaries, target frameworks, provider or transport support, migration
policy, compatibility, release/publishing, sample architecture, repository
workflow, or agent harness behavior. The label means the SpecKit constitution,
a feature spec, or stable docs may need to be updated before implementation.
Ordinary issues outside those surfaces should proceed without this label; it is
an architecture review signal, not a blanket implementation blocker.

## Issue Formats

Issue bodies should be specific enough for another maintainer or agent to
start without reconstructing the intent from chat. Do not include
machine-specific paths, local workspace paths, credentials, or handover-only
details in GitHub issues or comments.

### Bug

```markdown
## Problem

What is broken or surprising?

## Expected Behavior

What should happen?

## Actual Behavior

What happens instead?

## Reproduction

- Minimal steps, command, or scenario.

## Impact

Who or what is affected?

## Likely Area

Docs, package/API, persistence, transport, sample, release, or unknown.

## Verification

What check should pass after the fix?
```

Suggested labels: `bug` plus the relevant `area:*` label. Add
`architecture-review-required` only when the fix changes durable requirements,
architecture, sequencing, or package/public API policy.

### Feature Or Enhancement

```markdown
## Goal

What user or maintainer outcome should this enable?

## Context

Why is this needed now?

## Scope

- What is included.
- What is intentionally excluded.

## Expected Shape

Preferred package, API, docs, sample, or workflow shape if known.

## Completion Criteria

- Build/test/docs conditions that make this done.
```

Suggested labels: `enhancement` plus the relevant `area:*` label. Add
`architecture-review-required` when the feature affects durable architecture, public
API, or package boundaries.

### Cleanup

```markdown
## Goal

What should be simplified, removed, renamed, or made explicit?

## Current State

What exists today and why is it a problem?

## Compatibility Notes

What public API, package, docs, migration, or release-note concerns apply?

## Scope

- In scope.
- Out of scope.

## Completion Criteria

- Baseline, tests, docs, or release-note evidence.
```

Suggested labels: `type:cleanup` plus relevant `area:*` labels. Use
`architecture-review-required` for public API cleanup, package-boundary cleanup, or
any other compatibility-sensitive cleanup.

### Trial

```markdown
## Goal

What real-project, sample, or migration scenario should be tried?

## Scope

- What scenario or slice to exercise.
- What counts as enough signal.

## First Target Slice

The smallest meaningful slice to try first.

## Friction Tracking

Create separate issues for discovered Bondstone gaps. Each follow-up should
include observed scenario, expected path, actual friction, and likely owner.

## Non-Goals

What not to solve in this trial.

## Completion Criteria

- Verification performed.
- Follow-up issues created when needed.
- Summary comment added.
```

Suggested labels: `type:trial` plus relevant `area:*` labels.

## Working An Issue

1. Read the issue and project item state.
2. Move the item to `In Progress`.
3. Read relevant SpecKit constitution, feature artifacts, and consumer docs
   before changing code, docs, automation, public API, package boundaries,
   samples, or workflow.
4. Make the smallest coherent change or trial slice.
5. Create separate follow-up issues for distinct findings instead of adding
   backlog notes to repository docs.
6. Update consumer docs when user-facing behavior changes.
7. Update the SpecKit constitution, feature artifacts, or stable docs when
   durable requirements, architecture, governance, or implementation sequencing
   changes.
8. Verify with the narrow relevant command and, when needed, the repository
   quality gate.
9. Add a completion comment.
10. Close the issue and move the project item to `Done`.

## Completion Comment

Use this shape for implementation or trial completion comments:

```markdown
Summary:

- What changed or what was tried.

Verification:

- Commands, checks, or why an executable check was not relevant.

Follow-up issues:

- #123 Short title
- None.

Notes:

- Optional important context, deferred work, or residual risk.
```

Do not close issues merely because work started or because a plan exists.
Close only after the completion criteria are met or the issue is explicitly
closed as not planned, duplicate, or superseded.
