# Devcontainer

This devcontainer provides Bondstone's expected maintenance tooling: .NET 10,
Aspire, Docker-in-Docker, Node, pnpm, PowerShell, and ripgrep.

The container is an onboarding accelerator, not the source of truth. Durable
project knowledge lives in repository files under `docs/`, local README files,
and AGENTS files.

Frontend and browser-testing tooling is intentionally not installed. Add it
only when an accepted sample or testing ADR creates a concrete need.
