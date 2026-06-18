# Bondstone BMAD Guardrails

- Bondstone is a greenfield .NET library/framework for durable module
  boundaries, durable command sending, EF Core backed inbox/outbox persistence,
  operation observation, and transport adapters.
- Bondstone has no external production consumers yet. Do not assume legacy
  production tenants, account data, uptime commitments, or deployed customer
  environments.
- Treat Bondstone as a library/framework effort, not a product application,
  backend app, UI app, or SaaS product.
- Do not invent UI, auth, account-management, deployment, SaaS, product-app,
  onboarding, billing, or end-user account requirements unless the user
  explicitly asks for that bounded concern.
- Use native BMAD artifacts as the internal source of truth:
  `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md`,
  `_bmad-output/planning-artifacts/architecture.md`,
  `_bmad-output/planning-artifacts/epics.md`, and
  `_bmad-output/project-context.md`.
- Prefer package boundaries, public API shape, extension points, durable
  messaging semantics, persistence behavior, test strategy, examples,
  migration notes, compatibility posture, and architectural constraints.
- Keep `/docs` focused on consumer-facing and repository-operation guidance.
  Put internal runtime architecture and sequencing in BMAD artifacts.
