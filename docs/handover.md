# Handover Prompt

Use this document from a Codex session opened at the Bondstone repository root.

## Continuation Prompt

We are continuing the Bondstone extraction.

Read `AGENTS.md` first, then `docs/README.md`, `docs/adr/README.md`,
`docs/extraction.md`, `docs/packaging.md`, `docs/testing.md`,
`docs/repository.md`, and `docs/status.md`.

The historical source repository is source material only. Do not preserve
compatibility with it as a design constraint, and do not bulk-copy Bondstone
implementation code.

Current state:

- ADR-led maintenance is active. Check `docs/adr/README.md` before changing
  public API, package boundaries, target frameworks, provider or transport
  support, migration policy, compatibility, release/publishing, sample
  architecture, repository workflow, or agent harness behavior.
- Current package projects and dependency direction are recorded in
  `docs/packaging.md`.
- The current implemented and deferred runtime surface is summarized in
  `docs/status.md`.
- Package, testing, repository, sample, and architecture direction live in the
  stable docs under `docs/`.
- Default quality gate is `pnpm check`. Integration tests remain separate.

Next preferred work:

1. Read the current docs and source before editing.
2. Pick a narrow extraction or cleanup slice from `docs/extraction-plan.md` or
   the user's current request.
3. Create, amend, or supersede ADRs before widening durable decisions.
4. Run the narrowest useful verification first, then the repository quality
   gate when the change warrants it.
