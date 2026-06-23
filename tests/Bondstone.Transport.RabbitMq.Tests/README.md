# Bondstone.Transport.RabbitMq.Tests

Tests for the thin RabbitMQ transport adapter.

This project covers RabbitMQ builder behavior, native-driver envelope
dispatch, receive-worker handoff into Bondstone's durable receiver boundary,
and RabbitMQ Testcontainers-backed integration behavior.

See:

- [../../src/Bondstone.Transport.RabbitMq](../../src/Bondstone.Transport.RabbitMq)
  for package scope.
- [../../docs/architecture.md](../../docs/architecture.md) for
  transport-boundary behavior.
- [../../docs/testing.md](../../docs/testing.md) for test categories and
  verification entrypoints.
- [../../docs/packaging.md](../../docs/packaging.md) for adapter package
  policy.
