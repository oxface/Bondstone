# Architecture Agent Index

This folder contains stable runtime architecture docs.

Start with:

- [README.md](README.md) for topic routing.
- [messaging.md](messaging.md) for durable commands, integration events, inbox,
  outbox, receive pipelines, and transport boundaries.
- [persistence-core.md](persistence-core.md) before changing core persistence
  contracts.
- Provider-specific persistence or transport docs only when the task touches
  that provider.

Do not put future ideas here unless an accepted ADR makes them current
guidance. Use [../backlog/README.md](../backlog/README.md) for speculative or
campaign-sized work.
