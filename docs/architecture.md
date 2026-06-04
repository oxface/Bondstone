# Architecture

Bondstone architecture docs are split by topic so the stable contract stays
navigable as extraction proceeds.

## Index

- [architecture/README.md](architecture/README.md) records runtime positioning
  and durable boundary principles.
- [architecture/messaging.md](architecture/messaging.md) records durable
  command, message identity, and messaging-boundary rules.
- [architecture/persistence.md](architecture/persistence.md) records
  persistence-neutral outbox and inbox boundary rules.

## Application State

The architecture direction is accepted and documented. The first source
extraction slices now include stable message identity contracts, registry
behavior, durable command send contracts, durable operation read contracts, and
durable message envelopes, plus initial persistence-neutral outbox and inbox
contracts including operation-state storage, in `Bondstone`. EF Core
provider-neutral entities and model mappings have started. Broader source
extraction, samples, and service-split verification remain future application
work.
