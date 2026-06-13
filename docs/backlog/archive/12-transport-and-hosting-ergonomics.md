# Transport And Hosting Ergonomics

Archived: 2026-06-12

## Outcome

The transport and hosting ergonomics slice resolved as a documentation and
focused test-hardening pass.

- Durable route validation now includes provider-specific missing-route
  reasons.
- Normal provider transport setup is documented as the path that registers
  validators and diagnostics.
- Lower-level outbox transport overloads are documented as advanced
  manual-dispatch composition APIs.
- RabbitMQ receive failure logging is covered by integration tests.
- Service Bus receive failure logging is covered by integration tests.
- No broker provisioning helpers were added.
- No new public transport API was added.

Remaining transport and provisioning ideas have returned to
[../00-plans.md](../00-plans.md) as known pressure points only.
