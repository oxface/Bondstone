---
stepsCompleted: [1, 2]
inputDocuments:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md
  - _bmad-output/project-context.md
workflowType: "research"
lastStep: 3
research_type: "technical"
research_topic: "Epic 6 Durable Persistence And Receive Ledger"
research_goals: "Validate technology choices and implementation constraints for source outbox atomicity, durable receive transaction boundaries, EF/PostgreSQL production persistence, migration ownership, and provider-backed integration tests."
user_name: "Dude"
date: "2026-06-19"
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-06-19
**Author:** Dude
**Research Type:** technical

---

## Research Overview

This research supports Bondstone Epic 6: Durable Persistence And Receive
Ledger. It uses the current PRD, architecture, and Epic 6 stories as internal
source-of-truth inputs, then verifies technology-specific claims against
current public sources.

---

## Technical Research Scope Confirmation

**Research Topic:** Epic 6 Durable Persistence And Receive Ledger

**Research Goals:** Validate technology choices and implementation constraints
for source outbox atomicity, durable receive transaction boundaries,
EF/PostgreSQL production persistence, migration ownership, and provider-backed
integration tests.

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-06-19

## Technology Stack Analysis

### Programming Languages

Bondstone's Epic 6 stack is correctly centered on C# and modern .NET. The
repository targets `net10.0` with SDK `10.0.108`, and current Microsoft
download metadata lists .NET 10 as the latest LTS line with support through
November 14, 2028. That makes `net10.0` an appropriate library target for a
framework that wants current language/runtime features without taking an
unsupported runtime bet.

The durable persistence work should remain C#-native rather than introducing a
separate worker language. EF Core, Microsoft.Extensions hosting abstractions,
Npgsql, RabbitMQ.Client, Azure.Messaging.ServiceBus, xUnit, and Testcontainers
all have first-class .NET packages in the active repository stack. Keeping
claiming, ingestion, transaction participation, and tests in C# preserves type
identity rules and public API reviewability.

_Popular Languages:_ C# for the library/runtime, SQL for PostgreSQL-specific
claiming and uniqueness behavior, shell/Node only for repository orchestration.

_Emerging Languages:_ None should be added for Epic 6. The value is provider
semantics, not polyglot runtime expansion.

_Language Evolution:_ .NET 10 LTS supports the repository direction; package
metadata and official docs show the current ecosystem moving with .NET 10-era
EF Core and Npgsql packages.

_Performance Characteristics:_ C# is suitable for durable worker coordination,
while critical contention behavior belongs in PostgreSQL SQL statements and
constraints rather than in-memory coordination.

_Sources:_ https://dotnet.microsoft.com/en-us/download/dotnet,
https://dotnet.microsoft.com/en-us/platform/support/policy,
https://www.nuget.org/packages/microsoft.entityframeworkcore/,
https://www.nuget.org/packages/rabbitmq.client/

### Development Frameworks and Libraries

EF Core remains the central framework for module-owned persistence because it
provides change tracking, LINQ, updates, schema migrations, and transaction
APIs. Microsoft's transaction documentation confirms that database operations
within a committed transaction apply atomically and rollback leaves none of the
operations applied. That directly supports Story 6.1's source-state plus outbox
atomicity, provided the source state and outgoing rows are saved through the
same module `DbContext` transaction.

Npgsql.EntityFrameworkCore.PostgreSQL is the right provider for production
proofs. The provider is specifically the EF Core provider for PostgreSQL, and
its 10.0 line tracks EF Core 10 capabilities. Npgsql's concurrency-token docs
also point to PostgreSQL-specific behavior such as `xmin`, which matters for
provider-specific concurrency decisions, though Bondstone's durable identities
should still use explicit stable keys rather than hidden provider row versions.

RabbitMQ.Client and Azure.Messaging.ServiceBus should remain thin adapter
dependencies, not durable state managers. RabbitMQ documentation distinguishes
automatic and manual consumer acknowledgements, and recommends manual
acknowledgement with bounded prefetch for in-progress deliveries. Azure Service
Bus documentation describes locks and settlements, with complete/abandon/defer/
dead-letter as settlement operations. Those docs align with the architecture:
native settlement is a transport boundary, while Bondstone durable receive
state is the database-backed ledger.

_Major Frameworks:_ EF Core 10, Npgsql EF Core provider, Microsoft.Extensions
DI/hosting/logging/options abstractions, RabbitMQ.Client, Azure.Messaging.
ServiceBus.

_Micro-frameworks:_ Bondstone's own provider-neutral persistence and hosting
abstractions should stay small: module transaction runners, outbox writers,
incoming inbox ingestion stores, inspectors, claimers, and outcome recorders.

_Evolution Trends:_ EF Core and Npgsql are current with .NET 10; RabbitMQ's
modern .NET client is distributed through NuGet; Azure Service Bus guidance
continues to favor current Azure SDK libraries.

_Ecosystem Maturity:_ High. All core dependencies have official documentation,
NuGet distribution, and established .NET integration paths.

_Sources:_ https://learn.microsoft.com/en-us/ef/core/saving/transactions,
https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/,
https://www.npgsql.org/efcore/,
https://www.npgsql.org/efcore/release-notes/10.0.html,
https://www.rabbitmq.com/client-libraries/dotnet,
https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement

### Database and Storage Technologies

PostgreSQL should be the production proof database for Epic 6. PostgreSQL's
official docs cover transaction isolation, explicit locking, `INSERT ... ON
CONFLICT`, and `SELECT ... FOR UPDATE SKIP LOCKED`. The `SKIP LOCKED`
documentation is especially relevant because it explicitly calls out queue-like
tables as a suitable use case for skipping locked rows, while warning that the
result is an inconsistent view and not general-purpose read behavior.

For Story 6.1, durable outbox atomicity depends on source state and outgoing
rows sharing one transaction boundary. EF Core can express that transaction,
but the proof must be relational: commit, rollback, uniqueness, and terminal
outbox evidence need PostgreSQL-backed tests. For Story 6.2, incoming inbox
claiming and outcome mutation should lean on PostgreSQL row-level locking,
unique constraints, and compare-and-update semantics around status, claim
owner, lease expiry, and durable identity keys.

EF Core InMemory should stay out of relational proof. It is useful for fast
mapping and change-tracker tests, but it cannot prove PostgreSQL uniqueness,
transaction isolation, row locks, `SKIP LOCKED`, duplicate ingestion races, or
claim-owner mutation behavior.

_Relational Databases:_ PostgreSQL is the production durability target; other
relational providers are not Epic 6 proof targets.

_NoSQL Databases:_ NoSQL stores do not belong in Epic 6 unless a future PRD
adds provider-neutral durable persistence. The current scope depends on
relational transactions and constraints.

_In-Memory Databases:_ EF Core InMemory remains limited to fast mapping or
change-tracker boundaries, not durable correctness.

_Data Warehousing:_ Not applicable. The receive ledger and outbox are
operational tables, not analytics stores.

_Sources:_ https://www.postgresql.org/docs/current/transaction-iso.html,
https://www.postgresql.org/docs/current/explicit-locking.html,
https://www.postgresql.org/docs/current/sql-insert.html,
https://www.postgresql.org/docs/current/sql-select.html,
https://learn.microsoft.com/en-us/ef/core/saving/concurrency

### Development Tools and Platforms

The existing toolchain is appropriate: .NET SDK, centralized NuGet package
management, pnpm scripts as entrypoints, xUnit for tests, Testcontainers for
real infrastructure, and PublicApiGenerator for compatibility baselines. The
Epic 6 verification burden should expand in the PostgreSQL integration test
project rather than in unit tests that simulate relational behavior.

Testcontainers for .NET is a strong fit for Epic 6 because its PostgreSQL
module starts a PostgreSQL container from .NET tests, and its own guide frames
PostgreSQL integration testing as runnable from a normal developer workflow.
This matches the repository rule that PostgreSQL semantics require integration
tests.

Migration work should remain application-owned. EF Core's migration application
guidance emphasizes production review and SQL-script deployment benefits,
including DBA review and CI generation. That reinforces Story 6.3: Bondstone
can provide mappings and provider helpers, but should not ship automatic schema
rollout or runtime migration ownership.

_IDE and Editors:_ No Epic 6-specific IDE requirement. Normal .NET editor
support is enough.

_Version Control:_ Git plus public API baselines are sufficient. Avoid broad
metadata churn when adding integration proofs.

_Build Systems:_ Keep using repository `pnpm` scripts over ad hoc direct
commands. Use `pnpm backend:test:integration` for PostgreSQL-backed proofs.

_Testing Frameworks:_ xUnit and Testcontainers.PostgreSql for integration
coverage; EF InMemory only for fast non-relational boundaries.

_Sources:_ https://dotnet.testcontainers.org/modules/postgres/,
https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/,
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying,
https://xunit.net/docs/getting-started/netcore/cmdline,
https://www.nuget.org/packages/Testcontainers.PostgreSql

### Cloud Infrastructure and Deployment

Epic 6 is mostly persistence infrastructure, but deployment implications are
clear. Hosts own PostgreSQL provisioning, migrations, connection strings,
schema deployment, and operational maintenance. Bondstone should keep providing
library mappings, provider-specific helpers, workers, and inspection
contracts.

Broker infrastructure remains outside the durable ledger. RabbitMQ manual
acknowledgement and Azure Service Bus settlement semantics are relevant because
transport receive workers must not settle native messages as processed before
durable incoming inbox ingestion succeeds. After ingestion commits, retry and
terminal receive failure move to the durable inbox row; broker DLQ behavior is
not the Bondstone operator ledger.

Container infrastructure is useful for testing. Docker-backed Testcontainers
lets Epic 6 prove PostgreSQL behavior locally and in CI without requiring
developers to install PostgreSQL manually. Broader cloud platforms are not
needed for the Epic 6 proof unless a consumer trial introduces deployment
evidence.

_Major Cloud Providers:_ Azure matters for Service Bus adapter semantics, but
Epic 6's durable persistence proof is provider-database-first, not cloud-first.

_Container Technologies:_ Docker/Testcontainers are the preferred test
platform for PostgreSQL-backed integration tests.

_Serverless Platforms:_ Not relevant to the core Epic 6 implementation.

_CDN and Edge Computing:_ Not relevant.

_Sources:_ https://www.rabbitmq.com/docs/confirms,
https://www.rabbitmq.com/docs/consumers,
https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement,
https://dotnet.testcontainers.org/modules/postgres/

### Technology Adoption Trends

Current public adoption data supports the chosen stack without needing to
chase trends. Stack Overflow's 2025 developer survey technology page describes
continued interest in PostgreSQL, and its company summary reports Docker at
71% usage among cloud development and infrastructure technologies. That makes
PostgreSQL plus Docker-backed Testcontainers a pragmatic, widely familiar
verification path.

The trend that matters most for Bondstone is not adding more infrastructure,
but tightening the contract between EF Core, PostgreSQL, and native broker
drivers. Current docs across the stack point in the same direction: relational
transaction boundaries for atomic state changes, provider-specific SQL for
queue-like claiming, explicit migration ownership, and transport settlement
only after reliable application-side persistence.

_Migration Patterns:_ Consumer-owned EF migrations and reviewed SQL scripts
are the safest default for production schema rollout.

_Emerging Technologies:_ No emerging store or broker abstraction should be
introduced into Epic 6. The risk is over-generalization.

_Legacy Technology:_ Direct `inbox_messages` receive markers should remain
implementation detail while durable incoming inbox rows become the
operator-facing ledger.

_Community Trends:_ PostgreSQL and Docker/Testcontainers are well-aligned with
developer expectations for relational integration testing.

_Sources:_ https://survey.stackoverflow.co/2025/technology,
https://stackoverflow.co/company/press/archive/stack-overflow-2025-developer-survey/,
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying

## Integration Patterns Analysis

### API Design Patterns

Epic 6 should preserve Bondstone's current separation between immediate APIs,
durable send/publish APIs, and observation APIs. Durable command send is best
modeled as an acceptance API: the caller receives send metadata after the
source transaction accepts durable work into the outbox, while handler results
are observed through operation APIs. This aligns with the asynchronous
request-reply pattern in Azure Architecture Center, where long-running work is
accepted quickly and the caller later polls or observes completion state.

REST, GraphQL, and generic RPC are not core Epic 6 implementation surfaces.
They may sit outside Bondstone in a host application, but Epic 6's library API
should stay focused on explicit .NET contracts: durable command sender,
integration event publisher, outbox/inbox inspectors, persistence mappings,
claimers, and receive pipeline entrypoints. That keeps the durable boundary
usable from ASP.NET Core, workers, tests, or app-owned broker loops without
turning Bondstone into an HTTP API framework.

_RESTful APIs:_ Useful for host-facing operation status endpoints, especially
HTTP 202 plus polling/status resources for long-running accepted work.

_GraphQL APIs:_ No direct fit for Epic 6. GraphQL can query application state,
but it should not become the durable messaging contract.

_RPC and gRPC:_ No direct fit for the durable receive ledger. Synchronous RPC
would blur the accepted-work/result-observation split.

_Webhook Patterns:_ External webhook-style delivery should be translated by
the host into Bondstone durable envelopes and ingested before external
acknowledgement where durability matters.

_Sources:_ https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply,
https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design,
https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice

### Communication Protocols

Bondstone's integration pattern should remain protocol-adapter based. RabbitMQ
uses AMQP 0-9-1 concepts such as exchanges, queues, bindings, channels,
publisher confirms, and consumer acknowledgements. Azure Service Bus uses AMQP
1.0 as its primary protocol and exposes settlement operations through the
Azure SDK. Both protocols can move envelopes, but neither should be the
operator-facing ledger for Epic 6 outcomes.

The key receive rule is protocol-independent: transform a native delivery into
a durable envelope, resolve the receiver/binding, insert or detect a durable
incoming inbox row, commit that persistence boundary, and only then settle the
native broker delivery as accepted/processed. RabbitMQ manual acknowledgement
and Azure Service Bus complete/abandon/dead-letter semantics are transport
settlement tools, not replacements for durable inbox status.

_HTTP/HTTPS Protocols:_ Relevant for operation observation, admin APIs, and
host-owned integration endpoints, but not the internal durable ledger.

_WebSocket Protocols:_ Not relevant to Epic 6 durable persistence.

_Message Queue Protocols:_ AMQP 0-9-1 for RabbitMQ and AMQP 1.0 for Azure
Service Bus are the main native broker protocols in the current package set.

_gRPC and Protocol Buffers:_ Not required for Epic 6. Introducing them would
add a transport concern without improving database-backed durability.

_Sources:_ https://www.rabbitmq.com/tutorials/amqp-concepts,
https://www.rabbitmq.com/docs/publishers,
https://www.rabbitmq.com/docs/confirms,
https://www.rabbitmq.com/docs/consumers,
https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-protocol-guide,
https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement

### Data Formats and Standards

Epic 6 should keep durable envelope storage explicit and stable rather than
adopting a broad interoperability standard as the internal data model. Stable
message identity, handler identity, subscriber identity, durable kind, module
names, payload type, content type, correlation/operation metadata, status,
claim lease, attempt count, and failure reason need first-class columns or
stable serialized fields where the current architecture already expects them.

JSON remains the pragmatic payload format for inspection and application
interoperability, while binary formats such as Protobuf or MessagePack are not
needed for the persistence proof. CloudEvents is worth knowing as an external
event metadata standard: it standardizes common event metadata for routing and
interoperability across services. However, Bondstone already has durable
identity rules that are stricter than generic event portability. CloudEvents
may be a future adapter/input mapping, not the internal durable identity model.

_JSON and XML:_ JSON is the natural default for durable payload snapshots and
human-inspectable metadata. XML has no Epic 6-specific advantage.

_Protobuf and MessagePack:_ Efficient, but unnecessary for the immediate
durability and transaction-boundary stories.

_CSV and Flat Files:_ Not relevant to durable messaging runtime state.

_Custom Data Formats:_ Bondstone should continue using explicit
`DurableMessageEnvelope`-style records with stable identity fields rather than
CLR-type-derived identities.

_Sources:_ https://cloudevents.io/,
https://github.com/cloudevents/spec,
https://github.com/cloudevents/spec/blob/main/cloudevents/formats/json-format.md,
https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven

### System Interoperability Approaches

The strongest interoperability approach for Epic 6 is an anti-corruption layer
between host-owned infrastructure and Bondstone-owned durable records. Native
RabbitMQ, Azure Service Bus, local transport, or app-owned loops should adapt
incoming messages into Bondstone durable envelopes; Bondstone should not own
broker topology, broker retry, queue provisioning, or dead-letter policy.

For persistence, the interoperability seam is EF Core mapping plus
PostgreSQL-specific helpers. Consumers own their DbContext, schema choices,
migration generation, and deployment. Bondstone contributes mapping extensions,
provider services, and runtime pipeline contracts that participate in the
consumer's transaction boundary.

_Point-to-Point Integration:_ Durable commands are target-module point-to-point
messages, but the persisted outbox/inbox boundary must mediate delivery.

_API Gateway Patterns:_ Out of scope for Epic 6; host applications can add API
gateways above Bondstone.

_Service Mesh:_ Out of scope; service mesh does not replace database-backed
durability.

_Enterprise Service Bus:_ Bondstone should avoid becoming an ESB. It provides
module-boundary durable messaging and inspection, while hosts own broader
enterprise integration topology.

_Sources:_ https://learn.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer,
https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber,
https://learn.microsoft.com/en-us/azure/architecture/patterns/,
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying

### Microservices Integration Patterns

Epic 6 maps closely to established microservice data-consistency patterns, but
with a module-framework framing rather than a general service-mesh product.
The transactional outbox pattern addresses the core problem of atomically
updating local state and publishing a message without relying on a distributed
transaction across database and broker. Microservices.io also notes that a
message relay can publish more than once, which means consumers must be
idempotent. That supports Bondstone's durable receive keys and duplicate
ingestion behavior.

Saga guidance is useful context but should not expand Epic 6 scope. Azure's
Saga pattern guidance frames distributed consistency as a sequence of local
transactions with compensating actions. Bondstone can provide durable commands,
events, operations, and receive ledgers that an application saga might use, but
Epic 6 should not add saga orchestration or compensation policy.

_API Gateway Pattern:_ Host concern, not Bondstone durable persistence.

_Service Discovery:_ Host/deployment concern, not Epic 6.

_Circuit Breaker Pattern:_ Useful for host-owned broker/database clients, but
not a durable ledger feature.

_Saga Pattern:_ Relevant as a consumer pattern built on local transactions and
messages; Bondstone should not become a saga engine in Epic 6.

_Sources:_ https://microservices.io/patterns/data/transactional-outbox.html,
https://microservices.io/patterns/communication-style/idempotent-consumer.html,
https://microservices.io/patterns/data/saga.html,
https://learn.microsoft.com/en-us/azure/architecture/patterns/saga,
https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications

### Event-Driven Integration

Event-driven architecture guidance reinforces Bondstone's separation between
domain events and integration events. Azure describes event-driven systems as
event producers, consumers, and event channels. Microsoft .NET microservices
guidance describes integration events as synchronizing domain state across
microservices or external systems. Bondstone's architecture should preserve
that distinction: domain events are module-local facts, while integration
events are durable facts published outside the module boundary.

For Story 6.2, event-driven receive must be idempotent and ledger-backed.
Duplicate broker delivery after settlement failure, worker crash, or relay
replay should land on the same durable incoming inbox identity and avoid
running the handler twice. Retry, terminal failure, processed state, and stale
state should live on the durable inbox ledger rather than being scattered
across broker DLQs, direct `inbox_messages`, and operation rows.

_Publish-Subscribe Patterns:_ Integration events can fan out through
host-owned broker topology; subscriber identity belongs in durable receive
keys.

_Event Sourcing:_ Not an Epic 6 requirement. Durable inbox/outbox rows are
operational ledgers, not aggregate event streams.

_Message Broker Patterns:_ RabbitMQ and Azure Service Bus are supported as
thin native-driver adapters; durable truth remains in PostgreSQL.

_CQRS Patterns:_ Bondstone already separates immediate queries from command
and durable receive paths. Epic 6 should not let query execution write inbox,
outbox, operation, or event state.

_Sources:_ https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven,
https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber,
https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs,
https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications

### Integration Security Patterns

Security for Epic 6 is mostly ownership clarity. Hosts should own database
credentials, broker credentials, network policy, TLS settings, queue/topic
authorization, and secret rotation. Bondstone should avoid hiding credentials
inside durable abstractions or provisioning transport resources implicitly.

For persisted durable records, avoid treating payload visibility as harmless.
Outbox and inbox rows can contain business payloads, failure reasons,
correlation data, and operation identifiers. Consumers need deployment guidance
for database access controls, retention, purge/archive ownership, and logging
redaction. Transport-level security such as AMQP over TLS, Service Bus
credentials, or RabbitMQ permissions is necessary but not sufficient because
the durable ledger becomes the operational evidence store after native
settlement.

_OAuth 2.0 and JWT:_ Relevant to host APIs, not Bondstone persistence internals.

_API Key Management:_ Host-owned if exposing operation/admin endpoints.

_Mutual TLS:_ Infrastructure choice for host-owned service/broker/database
communication.

_Data Encryption:_ Consumer-owned database and broker configuration, with
Bondstone docs warning that durable rows may contain sensitive payloads.

_Sources:_ https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-protocol-guide,
https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement,
https://www.rabbitmq.com/tutorials/amqp-concepts,
https://www.rabbitmq.com/docs/confirms
