# Agent Skills

This folder contains repository-local agent skills for Bondstone maintainers.
Skills are reusable workflows, not a substitute for BMAD planning artifacts.

## Skill Rules

- If a workflow is repeated or likely to be repeated, make it a skill.
- Keep skills narrow, named after the job they help with, and grounded in
  versioned repository docs or BMAD artifacts.
- Durable technical decisions belong in the BMAD PRD, architecture, epics, or
  project-context according to their scope.
- Skills may summarize rules, but they should link back to durable BMAD
  artifacts instead of copying long architecture text.
- Do not create broad catch-all skills. Prefer several small skills with clear
  trigger descriptions.
- Do not embed stale package versions, current dates, or release-state guesses
  in skill bodies. Point to repository files or package metadata instead.

## Current Skill Set

- `bondstone-github-issue-workflow`: create, triage, prioritize, work, and
  complete GitHub Issues and Project items using the conventions in
  [docs/github-workflow.md](../../docs/github-workflow.md).

Add implementation-oriented skills only when a workflow proves reusable, for
example:

- package boundary changes;
- provider adapter additions;
- transport adapter additions;
- sample scenario maintenance;
- release and NuGet publishing validation;
- BMAD artifact maintenance.

## Skill Shape

Each skill should live at:

```text
.agents/skills/{skill-name}/
  SKILL.md
  agents/openai.yaml
```

`SKILL.md` should contain:

- frontmatter with `name` and `description`;
- a short "Read First" section;
- the workflow steps;
- expected outputs;
- verification guidance.
