# Handover Prompt

Use this prompt from a Codex session opened with `/home/oxface/repos/Bondstone`
as the workspace root.

```text
We are continuing the Bondstone extraction.

Read AGENTS.md first, then docs/README.md, docs/adr/README.md,
docs/extraction.md, docs/packaging.md, docs/testing.md, and docs/repository.md.

The historical source repository is ../net-react-modular-template. Treat it as
source material only. Do not preserve compatibility with it as a design
constraint, and do not bulk-copy Bondstone implementation code.

Current state:
- ADR 0001 is applied.
- ADRs 0002-0008 are accepted/amended and partially applied.
- The repo shell exists with Bondstone.slnx, net10.0 package projects, matching
  test projects, pnpm/Husky/commitlint/Prettier, CI, Release Please, and NuGet
  publish workflow.
- Publishing is intended to happen soon. Release Please owns the central
  VersionPrefix in Directory.Build.props. Check that RELEASE_PLEASE_TOKEN,
  the NUGET_USER repository variable, and the NuGet trusted publishing policy
  for .github/workflows/publish-nuget.yml are configured before relying on the
  release-to-publish path.
- Default quality gate is pnpm check. It runs formatting, restore, build,
  Unit/Application tests, and pack. Integration tests are separate.

Next preferred work:
1. Verify current repo state with pnpm install --frozen-lockfile and pnpm check.
2. Make the smallest required publishing-readiness fixes, updating ADRs/stable
   docs when release policy changes.
3. Start the first slow extraction slice: core abstractions and
   low-dependency primitives from ../net-react-modular-template into
   src/Bondstone, with neutral Unit/Application tests.
4. When design tension appears, create or amend ADRs before widening the code
   move.
```
