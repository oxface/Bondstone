# Bondstone.Transport.ServiceBus.Tests

Tests for the thin Azure Service Bus transport adapter.

This project covers Service Bus builder behavior, native-driver envelope
dispatch, receive-worker handoff into Bondstone's durable receiver boundary,
and Azure Service Bus Testcontainers-backed integration behavior.

See:

- [../../src/Bondstone.Transport.ServiceBus](../../src/Bondstone.Transport.ServiceBus)
  for package scope.
- [../../docs/architecture.md](../../docs/architecture.md) for
  transport-boundary behavior.
- [../../docs/testing.md](../../docs/testing.md) for test categories and
  verification entrypoints.
- [../../docs/packaging.md](../../docs/packaging.md) for adapter package
  policy.
