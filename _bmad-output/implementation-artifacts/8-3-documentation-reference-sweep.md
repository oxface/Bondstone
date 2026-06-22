---
baseline_commit: 2365cfcd47b8143d8108467093544c5f5ab894ee
---

# Story 8.3: Documentation Reference Sweep

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want stale references removed,
so that deleted decision, planning, architecture, and plan paths do not remain.

## Acceptance Criteria

1. Given active repository documentation is swept, when references are reviewed, then `rg` finds no active links to deleted paths.
2. Given retired workflow language remains, when it is kept, then it is clearly historical or scoped to already-completed implementation artifacts rather than current workflow guidance.
3. Given README, AGENTS, docs index, package READMEs, and test AGENTS are reviewed, when the story closes, then they route humans and agents to BMAD PRD, BMAD architecture, BMAD epics, project-context, and current consumer docs.

## Tasks / Subtasks

- [x] Confirm deleted path absence before editing. (AC: 1)
  - [x] Verify `docs/adr`, `docs/architecture`, and `docs/plans` directories are absent.
  - [x] Do not recreate deleted folders, placeholder indexes, or compatibility redirects to satisfy stale links.
- [x] Sweep active human and agent documentation surfaces. (AC: 1, 2, 3)
  - [x] Run stale-path sweeps over `README.md`, `AGENTS.md`, `docs`, `src`, `tests`, and `samples`.
  - [x] Include active scoped indexes and package pages: `docs/README.md`, `docs/AGENTS.md`, `src/README.md`, `src/*/README.md`, `tests/**/AGENTS.md`, `tests/**/README.md`, `samples/AGENTS.md`, and `samples/README.md`.
  - [x] Treat matches under `_bmad-output/implementation-artifacts` as historical story records unless they route current workflow or are copied into active docs.
  - [x] Treat generic reusable skill examples under `.agents/skills` as non-actionable unless they specifically route Bondstone work to deleted Bondstone paths.
- [x] Replace active stale references with current authorities. (AC: 1, 3)
  - [x] Replace deleted `docs/adr/**` or decision-file workflow references with BMAD PRD/architecture/epics routing or GitHub Issues/Projects guidance as appropriate.
  - [x] Replace deleted `docs/architecture/**` links with `_bmad-output/planning-artifacts/architecture.md` when the reference is about internal runtime architecture.
  - [x] Replace deleted `docs/plans/**` links with BMAD epics, sprint status, GitHub Issues/Projects, or current repository docs depending on the work-tracking context.
  - [x] Keep `/docs` consumer-facing; do not move internal durable architecture back into consumer docs.
- [x] Preserve package and discovery alignment from Story 8.2. (AC: 3)
  - [x] Keep `docs/packaging.md` as package policy owner and `docs/package-discovery.md` as package/namespace discovery owner.
  - [x] Do not rewrite package policy, versioning, publishing, public API classification, or package README purpose text unless a stale reference forces a narrow correction.
  - [x] If package README links change, keep repository links as absolute GitHub URLs so NuGet README rendering remains useful.
- [x] Verify and record results. (AC: 1, 2, 3)
  - [x] Run targeted stale-reference commands listed in Dev Notes and record whether remaining matches are historical, generic reusable-skill examples, or actionable.
  - [x] Run `pnpm format:check` for docs/story formatting.
  - [x] Run `pnpm backend:pack` if package README content, package metadata, XML docs, or package artifact behavior changes.
  - [x] Run `pnpm check` if the cleanup broadens beyond narrow docs/reference edits.

### Review Findings

- [x] [Review][Patch] Record that review covered the combined Story 8.2 and Story 8.3 working-tree diff — User chose to treat this review as covering the combined uncommitted Story 8.2 and Story 8.3 work after the baseline diff included `_bmad-output/implementation-artifacts/8-2-package-policy-and-discovery-documentation.md`, `docs/package-discovery.md`, `docs/packaging.md`, and `src/README.md` alongside the Story 8.3 files.
- [x] [Review][Patch] Add a relative-link stale-path sweep so deleted docs paths are not missed [_bmad-output/implementation-artifacts/8-3-documentation-reference-sweep.md:103]
- [x] [Review][Patch] Include `retired decision` in the broader explanatory sweep [_bmad-output/implementation-artifacts/8-3-documentation-reference-sweep.md:116]

## Dev Notes

Story 8.3 is a final reference-hygiene slice for FR2.5. It should not become another package policy rewrite, public API review, runtime behavior change, or architecture migration. The expected implementation is either a verified no-op plus recorded evidence, or a narrow edit to active docs that still point at deleted paths.

### Current State Intelligence

- `docs/adr`, `docs/architecture`, and `docs/plans` are absent in the current tree.
- A targeted active-surface sweep over `README.md`, `AGENTS.md`, `docs`, `src`, `tests`, and `samples` found no matches for `docs/architecture`, `docs/adr`, `docs/plans`, `adr-required`, `bondstone-adr`, `decision-file review`, `Use ADR skills`, `ADRs preserve durable decisions`, or `non-native planning`.
- A broader sweep that includes `.agents/skills` found only generic reusable skill examples:
  - `.agents/skills/bmad-document-project/workflows/full-scan-instructions.md` mentions `docs/architecture/` as a generic scan candidate.
  - `.agents/skills/wds-5-agentic-development/steps-a/step-04-document-findings.md` mentions `docs/architecture/` in a generic analyzed-project example.
    These are not active Bondstone routing by themselves; do not rewrite generic skill internals for this story unless they instruct Bondstone work to use deleted Bondstone paths.
- A broader sweep that includes `_bmad-output/implementation-artifacts` finds many historical story files quoting old cleanup criteria and commands. Those are implementation records, not current source-of-truth routing. Do not churn completed story records merely to reduce grep output unless a current doc copies them as active instruction.
- `docs/README.md` already names current docs and BMAD artifacts as the documentation model.
- `docs/AGENTS.md` already points internal architecture work to BMAD architecture and keeps `/docs` consumer-facing.
- Root `README.md` already links BMAD PRD, architecture, epics, project-context, package policy, package discovery, setup, operations, observability, public API, repository, samples, and testing docs.
- Root `AGENTS.md` already routes product scope, runtime architecture, package boundaries, public API strategy, testing, samples, and GitHub workflow to current BMAD/docs owners.
- `src/README.md` and `src/*/README.md` already link package policy and package discovery; package READMEs use absolute GitHub URLs for repository docs, which is appropriate for NuGet rendering.
- `tests/**/AGENTS.md` and `tests/**/README.md` already point to `docs/testing.md`, BMAD architecture where package behavior matters, and current package docs without deleted architecture/ADR routes.
- `samples/AGENTS.md` and `samples/README.md` already point to `docs/samples.md`, `docs/testing.md`, and current sample/test paths.

### Files To Read Or Update

Read these UPDATE files completely before changing them:

- `README.md` - root BMAD and docs routing.
- `AGENTS.md` - root agent source-of-truth routing and operating rules.
- `docs/README.md` - docs ownership model and index.
- `docs/AGENTS.md` - documentation-folder agent routing.
- `docs/repository.md` - repository layout, context-index convention, work tracking, and docs/plans guidance.
- `docs/github-workflow.md` - GitHub issue/project tracking if stale plan or decision workflow routing appears.
- `src/README.md` and `src/*/README.md` - package index and package README link surfaces.
- `tests/AGENTS.md`, `tests/**/AGENTS.md`, and `tests/**/README.md` - test documentation routing.
- `samples/AGENTS.md` and `samples/README.md` - sample documentation routing.
- `.agents/skills/AGENTS.md` and any specific repository-local skill file only if the sweep finds active Bondstone deleted-path routing under `.agents/skills`.
- `docs/packaging.md`, `docs/package-discovery.md`, and `docs/public-api.md` only if a stale reference appears in those files; otherwise leave Story 8.2 and Story 8.1 outputs alone.

### Architecture Compliance

- BMAD planning artifacts own internal product requirements, runtime architecture, implementation sequence, and lean agent rules.
- `/docs` owns consumer-facing and repository-operation guidance; it should link to BMAD architecture for internal durable behavior instead of duplicating architecture books.
- Deleted `docs/adr/**`, `docs/architecture/**`, and `docs/plans/**` paths must not be restored as current sources.
- GitHub Issues and GitHub Projects track backlog work, real-project findings, cleanup tasks, prioritization, and ownership. Durable requirements, architecture, or sequence changes update BMAD artifacts.
- Keep reference cleanup narrow. Do not change runtime source, public APIs, baselines, package IDs, versioning, publishing policy, or release metadata for this story unless an active stale reference is inseparable from the correction.

### Suggested Reference Sweep Commands

Use fixed-string commands for exact deleted paths and a regex command for workflow terms. `rg`'s default regex engine does not support lookaround; use `--fixed-strings` for literal path sweeps or `--pcre2` only if a pattern truly needs PCRE2 features.

```bash
for d in docs/adr docs/architecture docs/plans; do
  if [ -d "$d" ]; then echo "exists $d"; else echo "absent $d"; fi
done
```

```bash
rg -n --fixed-strings "docs/adr" README.md AGENTS.md docs src tests samples _bmad-output/project-context.md _bmad-output/planning-artifacts -g '*.md' -g 'AGENTS.md'
rg -n --fixed-strings "docs/architecture" README.md AGENTS.md docs src tests samples _bmad-output/project-context.md _bmad-output/planning-artifacts -g '*.md' -g 'AGENTS.md'
rg -n --fixed-strings "docs/plans" README.md AGENTS.md docs src tests samples _bmad-output/project-context.md _bmad-output/planning-artifacts -g '*.md' -g 'AGENTS.md'
```

```bash
rg -n "\]\((\.\./)*(adr|architecture|plans)/|\]\(/docs/(adr|architecture|plans)/" README.md AGENTS.md docs src tests samples _bmad-output/project-context.md _bmad-output/planning-artifacts -g '*.md' -g 'AGENTS.md'
```

```bash
rg -n "adr-required|bondstone-adr|decision-file review|Use ADR skills|ADRs preserve durable decisions|non-native planning|retired decision" README.md AGENTS.md docs src tests samples _bmad-output/project-context.md _bmad-output/planning-artifacts -g '*.md' -g 'AGENTS.md'
```

If the active-surface commands are clean, optionally run the broader explanatory sweep below and classify remaining matches instead of editing historical records:

```bash
rg -n "docs/architecture|docs/adr|docs/plans|adr-required|bondstone-adr|decision-file review|Use ADR skills|ADRs preserve durable decisions|non-native planning|retired decision" README.md AGENTS.md docs src tests samples .agents/skills _bmad-output/implementation-artifacts -g '*.md' -g 'AGENTS.md' -g 'SKILL.md'
```

### Previous Story Intelligence

Story 8.2 established:

- `docs/packaging.md` is the central package policy reference for package IDs, target framework, dependency direction, artifacts, versioning, publishing, and migration/release coordination.
- `docs/package-discovery.md` is the central capability-to-package and namespace reference.
- Root, docs, source indexes, and package READMEs point to both central references.
- Package READMEs use absolute GitHub URLs for docs and tests because they render outside the repository in NuGet/package-manager surfaces.
- Docs-only package policy changes should not touch public API baselines.

Story 8.1 established:

- Eight active packable packages, public API baseline package list, and checked-in baselines are aligned.
- Production package collaboration remains through explicit contracts or package-local implementation; `InternalsVisibleTo` remains test-only.
- Public API baseline diffs are review evidence, not approval. Story 8.3 should not refresh baselines unless it unexpectedly changes public/protected API shape, which it should not.

Stories 2.1-4.3 established:

- Retired non-native planning, decision-file, duplicate architecture, docs index, source README/AGENTS, test README/AGENTS, and GitHub workflow cleanup already happened.
- If an active stale reference appears, replace it with current BMAD architecture, PRD, epics, project-context, consumer docs, or GitHub Issues/Projects guidance. Do not restore deleted folders to make links pass.

### Git Intelligence

Recent commits at story creation time:

- `2365cfc docs: missed docs`
- `f0fd433 docs: public api`
- `4b07a5c fix: diagnostics improvement`
- `fb42e57 docs: more tightening`
- `8a7090d fix: more observation`

Recent work favors narrow documentation/runtime corrections with targeted verification. Continue that pattern: classify matches carefully and avoid churn in historical story artifacts.

### Latest Technical Information

No dependency upgrade is required for this story. Use repository-pinned tools and local command behavior.

- Local `rg` is `ripgrep 15.1.0` with PCRE2 available. Default ripgrep syntax rejected lookaround during story creation; prefer literal `--fixed-strings` sweeps for deleted path checks.
- ripgrep's official README describes `rg` as a recursive line-oriented regex search tool that respects gitignore rules by default and skips hidden and binary files by default. Source: https://github.com/BurntSushi/ripgrep
- The ripgrep guide documents glob and file-type filtering as the normal way to constrain searches. Source: https://github.com/BurntSushi/ripgrep/blob/master/GUIDE.md
- NuGet MSBuild pack docs say `PackageReadmeFile` points to a Markdown readme within the package and the file must be explicitly packed. If package README links are touched, keep them render-safe for NuGet. Source: https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets

### Project Structure Notes

- Active docs and agent indexes are the primary implementation surface.
- Historical implementation artifacts can mention retired paths as evidence of past cleanup. Do not edit completed story files solely to remove historical command examples.
- Consumer-facing docs under `/docs` should remain current docs, not internal architecture books.
- Package README edits, if any, may affect package artifacts and should trigger `pnpm backend:pack`.
- Formatting-only or narrow docs changes should run `pnpm format:check`; broader cleanup can run `pnpm check`.

### Project Context Reference

Project context says Bondstone is a .NET library/framework for durable module boundaries, durable command sending, EF Core backed inbox/outbox persistence, operation observation, and transport adapters. It also says README files orient humans, AGENTS files orient agents, GitHub Issues/Projects track backlog and cleanup work, and BMAD artifacts own durable requirements, architecture, sequencing, and lean implementation guardrails.

### Open Questions

None blocking. During implementation, decide whether broad historical matches under `_bmad-output/implementation-artifacts` should be recorded as expected historical evidence or whether any specific line is being used as current guidance. Default to preserving historical story records.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-22: Activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-22: Loaded project context, config, sprint status, create-story workflow assets, checklist, Epic 8, PRD FR2/FR9/FR10 sections, architecture documentation ownership and verification sections, root/docs/source/test/sample indexes, previous stories 8.1 and 8.2, recent git history, current local ripgrep version, and current external docs for ripgrep and NuGet package README behavior.
- 2026-06-22: Confirmed story key `8-3-documentation-reference-sweep`; `epic-8` is already `in-progress`, and story status should move from `backlog` to `ready-for-dev`.
- 2026-06-22: Confirmed no UX artifact exists for this library documentation story.
- 2026-06-22: Confirmed existing local edits from Story 8.2 in sprint status and docs; this story creation must preserve them.
- 2026-06-22: Dev-story activation resolver failed because local `python3` could not import stdlib `json`; workflow customization was resolved manually from base/team/user TOML fallback. No prepend or append steps configured.
- 2026-06-22: Marked `8-3-documentation-reference-sweep` in progress in sprint status and story status while preserving existing `baseline_commit`.
- 2026-06-22: Read active root/docs/source/test/sample documentation surfaces and BMAD PRD/architecture/epics sections governing FR2.5 and documentation ownership.
- 2026-06-22: Verified `docs/adr`, `docs/architecture`, and `docs/plans` are absent.
- 2026-06-22: Active docs-only sweeps over `README.md`, `AGENTS.md`, `docs`, `src`, `tests`, and `samples` returned no matches for deleted paths or retired workflow terms.
- 2026-06-22: Sweeps including BMAD planning artifacts found only current criteria/verification mentions of deleted paths and retired workflow language, not active routing links.
- 2026-06-22: Broader sweep found historical implementation-story records, this story's own checklist/dev notes, and generic reusable skill examples under `.agents/skills`; no actionable Bondstone routing remained.
- 2026-06-22: `pnpm format:check` passed.
- 2026-06-22: `pnpm check` passed, including format, restore, Release build, fast tests, pack, and package tests.
- 2026-06-22: Code review ran against the combined uncommitted Story 8.2 and Story 8.3 working-tree diff by user decision; review findings were applied by adding a relative-link stale-path sweep and expanding the broader explanatory sweep to include `retired decision`.
- 2026-06-22: Review follow-up verification passed: relative-link stale-path sweep returned no matches, broader explanatory sweep returned only historical implementation records and known generic skill examples, and `pnpm format:check` passed.

### Implementation Plan

- Treat Story 8.3 as a reference-hygiene verification slice unless an active stale reference is found.
- Use fixed-string path sweeps for deleted directories and regex sweeps for retired workflow terms.
- Classify remaining matches by surface: active docs, BMAD criteria/verification text, historical implementation records, or generic reusable skill examples.
- Avoid package policy, public API, runtime, baseline, and README purpose changes unless an active stale reference requires a narrow correction.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.3 is ready for dev with current stale-reference sweep intelligence, exact files to read, narrow cleanup guardrails, and verification commands.
- Confirmed deleted `docs/adr`, `docs/architecture`, and `docs/plans` directories remain absent; no replacement folders or redirects were created.
- Reviewed root README/AGENTS, docs indexes, repository and GitHub workflow docs, source package READMEs, test AGENTS/READMEs, and sample docs for current routing.
- Active documentation surfaces had no stale deleted-path links or retired workflow instructions, so no active docs required edits.
- Remaining broad-sweep matches are classified as BMAD criteria/verification text, historical implementation records, this story's own evidence, or generic reusable skill examples; none route current Bondstone work to deleted paths.
- Review follow-up added a relative-link deleted-path sweep and included `retired decision` in the broader explanatory sweep command.
- Preserved Story 8.2 package/discovery alignment by leaving package policy, package discovery, public API docs, package README purpose text, and NuGet-rendered absolute package README links unchanged.
- Verification passed with `pnpm format:check` and full `pnpm check`.

### File List

- `_bmad-output/implementation-artifacts/8-3-documentation-reference-sweep.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-22: Created story 8.3 documentation reference sweep context and updated sprint status to `ready-for-dev`.
- 2026-06-22: Completed documentation reference sweep; verified no active stale references remained and updated story/sprint status for review.

### Story Completion Status

Done.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 8 and Story 8.3 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR2.5 documentation deduplication and FR10.5 documentation-only verification.
- `_bmad-output/planning-artifacts/architecture.md` - Documentation Ownership and Verification Strategy.
- `_bmad-output/project-context.md` - documentation, workflow, testing, and agent-routing guardrails.
- `README.md` - root BMAD and docs routing.
- `AGENTS.md` - root agent index and operating rules.
- `docs/README.md` and `docs/AGENTS.md` - docs ownership and docs-folder agent routing.
- `docs/repository.md` - context-index convention and work-tracking guidance.
- `docs/packaging.md` - package policy owner.
- `docs/package-discovery.md` - package and namespace discovery owner.
- `src/README.md` and `src/*/README.md` - package indexes and package README link surfaces.
- `tests/AGENTS.md`, `tests/**/AGENTS.md`, and `tests/**/README.md` - test routing surfaces.
- `samples/AGENTS.md` and `samples/README.md` - sample routing surfaces.
- `_bmad-output/implementation-artifacts/8-1-public-api-and-package-collaboration-validation.md` - previous Epic 8 public API and package collaboration learnings.
- `_bmad-output/implementation-artifacts/8-2-package-policy-and-discovery-documentation.md` - previous Epic 8 package policy/discovery learnings.
