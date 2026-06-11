# Bondstone.Capabilities.DomainEvents

Optional module-local domain event capability contracts for Bondstone.

This package is contracts-only. It is not a domain event bus, does not dispatch
handlers by itself, and does not persist domain events without a provider
bridge such as `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`.

See [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md).
