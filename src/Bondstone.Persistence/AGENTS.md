# Bondstone.Persistence Agent Index

This folder contains provider-neutral durable persistence contracts and
outbox/inbox primitives.

Start with:

- [README.md](README.md) for package scope.
- [../../docs/packaging.md](../../docs/packaging.md) for package IDs and
  dependency direction.
- [../../docs/architecture.md](../../docs/architecture.md) before changing
  durable persistence, outbox, inbox, operation state, or package boundaries.
- [../../docs/testing.md](../../docs/testing.md) before adding or moving tests.

Keep this package provider-neutral. Concrete EF Core and PostgreSQL behavior
belongs in sibling persistence provider packages.
