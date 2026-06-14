---
name: bondstone-github-issue-workflow
description: Work with Bondstone GitHub Issues and GitHub Projects as the backlog source. Use when Codex needs to create, triage, prioritize, implement, comment on, close, or move Bondstone issues/project items; write bug, feature, cleanup, or trial issue bodies; apply labels; or capture follow-up issues from implementation or real-project trials.
---

# Bondstone GitHub Issue Workflow

Use this skill for Bondstone issue and project work. GitHub Issues and
Projects are the work tracker; repository docs describe current behavior; ADRs
preserve durable decisions.

## Read First

- [AGENTS.md](../../../AGENTS.md)
- [docs/README.md](../../../docs/README.md)
- [docs/github-workflow.md](../../../docs/github-workflow.md)
- [docs/adr/README.md](../../../docs/adr/README.md) when the issue is
  `adr-required` or affects durable technical decisions

## Workflow

1. Read the issue body, labels, comments when useful, and project item status.
2. Check live labels before inventing labels. Prefer existing `bug`,
   `enhancement`, `documentation`, `type:*`, `area:*`, and `adr-required`
   labels.
3. Move selected work to `In Progress` before implementation.
4. Read relevant stable docs and ADRs before changing code, docs, automation,
   public API, package boundaries, samples, or workflow.
5. Use the issue body formats in [docs/github-workflow.md](../../../docs/github-workflow.md)
   when creating bug, feature, cleanup, or trial issues.
6. Keep local workspace paths, credentials, and handover-only details out of
   GitHub issue bodies and comments.
7. Create separate follow-up issues for distinct findings instead of adding
   backlog notes to repository docs.
8. Update stable docs when current behavior changes.
9. Use ADR skills when an issue creates, changes, supersedes, or archives a
   durable technical decision.
10. Add a completion comment with summary, verification, follow-up issues, and
    residual notes.
11. Close the issue and move the project item to `Done` only after completion
    criteria are met.

## Issue Creation Guidance

Use the stable doc's templates rather than improvising long prose. Choose:

- Bug: broken or surprising behavior with reproduction and expected/actual.
- Feature/enhancement: new capability with goal, scope, expected shape, and
  completion criteria.
- Cleanup: simplification/removal/internalization with compatibility notes.
- Trial: real-project, sample, or migration exercise with friction tracking.

Apply labels that describe the issue type and area. Add `adr-required` when
the issue affects public API, package boundaries, target frameworks, provider
or transport support, migration policy, compatibility, release/publishing,
sample architecture, repository workflow, or agent harness behavior.

## Project Status Guidance

Use the project status field this way:

- `Inbox`: captured, not prioritized.
- `Ready`: clear enough to start.
- `In Progress`: actively being worked.
- `Blocked`: external input or state is required.
- `Done`: verified and closed or otherwise resolved.

For GitHub Project single-select fields through MCP, use option ids when
display-name updates fail.

## Output

Report:

- issue number and project status changes;
- labels applied or intentionally left unchanged;
- stable docs, ADRs, or follow-up issues created;
- verification performed;
- remaining work, blockers, or residual risk.

## Verification

For issue-only changes, read back the created or updated issue/project item.
For docs or code changes, run the narrow relevant check and `git diff --check`;
run broader repository verification when the change warrants it.
