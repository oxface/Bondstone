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
- Publishing to nuget.org works through Release Please, GitHub Releases, and
  NuGet trusted publishing. GitHub Packages is not a current target. Release
  Please owns the central VersionPrefix in Directory.Build.props.
- Default quality gate is pnpm check. It runs formatting, restore, build,
  Unit/Application tests, and pack. Integration tests are separate.

Next preferred work:
1. Verify current repo state with pnpm install --frozen-lockfile and pnpm check.
2. Start the first slow extraction slice: core abstractions and
   low-dependency primitives from ../net-react-modular-template into
   src/Bondstone, with neutral Unit/Application tests.
3. When design tension appears, create or amend ADRs before widening the code
   move.
```
