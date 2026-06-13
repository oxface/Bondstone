# Real Project Readiness

Archived: 2026-06-13
Extracted: 2026-06-13

## Outcome

The real project readiness slice resolved as an adoption documentation pass.

- The root README now points new adopters to the normal setup path and package
  README quick guides.
- `docs/setup.md` now includes a concise package-selection path for normal
  PostgreSQL-backed hosts, direct transport adapters, hosted outbox worker
  composition, optional capability packages, and verification entrypoints.
- Package READMEs now include quick paths that distinguish normal setup from
  advanced provider-neutral contracts and lower-level composition surfaces.
- `docs/samples.md` and `samples/README.md` now document service-split
  readiness over the existing modular-monolith sample.
- The bounded service-split path keeps `fulfillment` as the first future host
  extraction candidate and uses RabbitMQ as the preferred direct-provider
  boundary proof when needed.
- No runtime behavior, public API, package boundary, or target framework was
  changed.
- No broker provisioning helpers were added.
- No new sample host was added.

No ADR was required because this slice only updated documentation for current
behavior and sample direction. A later service-split sample host still needs a
concrete verification reason and ADR review if it changes durable sample
architecture.
