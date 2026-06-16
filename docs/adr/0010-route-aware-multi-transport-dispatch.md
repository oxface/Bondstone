# 0010 Route-Aware Multi-Transport Dispatch

Status: Accepted
Application: Partially Applied
Date: 2026-06-16

## Context

ADR 0008 reintroduced RabbitMQ and Azure Service Bus as thin native-driver
adapter packages. Those adapters intentionally avoid topology ownership,
provider-neutral diagnostics, retry policy, subscription storage, and broker
runtime behavior.

This leaves an expected next question: can one Bondstone host use multiple
transports at the same time? That will likely be requested by consumers during
service extraction, broker migration, cloud/on-prem integration, or when
commands and integration events are intentionally delivered through different
broker products.

Bondstone is vendor-agnostic at the durable envelope boundary, but a simple
message-type-to-dispatcher dictionary is too weak. Commands are directed to a
target module. Events may be routed by message identity, source module,
environment, migration phase, or subscriber topology selected outside
Bondstone. The same event type may also intentionally appear on more than one
transport during a migration. Broker receive is different again: the native
consumer or subscription has already selected the delivery source before
Bondstone receives the envelope.

## Decision

Bondstone's current MVP contract is one normal outbound dispatcher per
service provider. Built-in dispatcher extensions such as local transport,
RabbitMQ, Service Bus, and app-owned dispatcher registration are normal
single-dispatcher composition choices. A host that needs multiple outbound
transports must register one explicit aggregate dispatcher instead of relying
on implicit adapter accumulation.

The accepted future direction for first-class outbound multi-transport support
is route-aware dispatch over durable envelopes, not topology ownership and not
a CLR-type-only map.

An outbound multi-transport dispatcher must route from persisted
`DurableOutboxRecord` or `DurableMessageEnvelope` data. Routes may consider:

- `MessageKind`;
- stable `MessageTypeName`;
- `SourceModule`;
- `TargetModule` for commands;
- partition key or explicit metadata when the application records such policy.

Commands should normally route by message kind, stable command identity, and
target module. Events should normally route by message kind, stable event
identity, and source module or app-owned publication policy. Event subscriber
topology remains native/app-owned; Bondstone does not infer broker
subscriptions from module subscribers.

Exactly one outbound route must own a claimed outbox record. Zero matching
routes and multiple matching routes are configuration or dispatch errors and
must fail loudly. A route-aware dispatcher may improve this with startup
validation, but it must not provision topology or make ambiguous delivery
choices silently.

Inbound multi-transport is additive only at the native receive boundary.
Broker-specific receive workers or app-owned native consumers may coexist in
one host when explicitly registered, but each native delivery still calls the
shared `IDurableEnvelopeReceiver`. Command receive uses the envelope target
module. Event receive must supply the subscriber module and stable subscriber
identity selected by the native subscription. Bondstone does not maintain a
provider-neutral receive topology map.

The existing `IDurableEnvelopeDispatchRoute` and
`RoutedDurableEnvelopeDispatcher` are the seed of this model for advanced
composition. Thin RabbitMQ and Azure Service Bus adapters do not yet expose
route registration helpers; adding those helpers requires focused design and
tests but does not require Bondstone to own broker topology.

## Consequences

The MVP remains simple: choose one outbound dispatcher per host unless the
application explicitly owns aggregate dispatch.

The future multi-transport path is compatible with the current durable
envelope model. It can support broker migration, command/event split
delivery, or per-module transport choices without coupling Bondstone to
RabbitMQ exchanges, Service Bus topics, Rebus endpoints, or another provider's
topology model.

The model deliberately rejects a bare message-type-to-dispatcher map as the
public contract. Message identity is important, but command target module,
event source module, partition metadata, and explicit app-owned policy are
also part of real routing decisions.

Future work may add public route-builder ergonomics for RabbitMQ and Service
Bus, startup conflict validation, and sample tests that prove aggregate
dispatch over two transports. That work should stay below the topology line:
it may choose a dispatcher for a durable envelope, but it must not create or
validate provider-native broker topology as Bondstone-owned state.

## Related Decisions

- Narrows [0008 Thin Broker Adapters](0008-thin-broker-adapters.md).
- Amends [0005 Transport Adapters And Receive Helpers](0005-transport-adapters-and-receive-helpers.md).
- Relates to [0002 Library Scope And Package Surface](0002-library-scope-and-package-surface.md).

## Application Notes

- Current contract: normal hosts register one outbound dispatcher. Multiple
  inbound receive workers may be explicitly registered, but inbound topology
  remains native/app-owned.
- Stable docs: messaging architecture, setup, package discovery, and
  packaging docs describe the one-dispatcher MVP rule and the explicit
  route-aware aggregate path.
- Agent guidance: root `AGENTS.md` and architecture `AGENTS.md` already
  require ADR review before broad changes to transport support, package
  boundaries, or runtime architecture.
- Application evidence: `IDurableEnvelopeDispatchRoute` and
  `RoutedDurableEnvelopeDispatcher` already provide a low-level explicit
  route-aware aggregate that fails loudly on zero or ambiguous matches. Built
  in RabbitMQ and Service Bus dispatcher extensions still register the normal
  single `IDurableEnvelopeDispatcher` slot.
- Pending or deferred: adapter-level route-builder helpers, startup route
  conflict validation, and two-broker sample tests are deferred until real
  consumer demand justifies the ergonomics.

## Verification

ADR-only change. Read current ADR, architecture, setup, packaging,
package-discovery, and testing docs; checked existing dispatcher and route
implementation shape.
