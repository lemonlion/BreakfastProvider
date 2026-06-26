# Remaining parity — Reporting event-ingestion features

Everything in the C# scenario inventory is mirrored in the Java twin **except** three Reporting features
that ingest events from secondary channels and expose them via additional GraphQL queries. They are a
distinct build-out (new JPA stores + GraphQL queries + event consumers); this is the design and the
verification plan for each, split by what can be exercised by the local docker-mode suite.

Current Reporting GraphQL surface in the Java twin: `orderSummaries`, `popularRecipes` (both implemented
and tested across all four frameworks). The C# `ReportingQuery` additionally exposes `recipeReports`,
`batchCompletions`, `ingredientShipments`, `equipmentAlerts`.

> **STATUS:** #1 EventGrid_Webhook / `ingredientShipments` and #2 Batch_Completions / `batchCompletions`
> (real Pub/Sub publish + consumer) are implemented and green across all four frameworks. #3
> Equipment_Alerts / `equipmentAlerts` is being upgraded to the **real Azure Event Hubs transport**.
>
> **CORRECTION (was wrong earlier):** Azure Event Hubs *does* have a maintained local emulator —
> `mcr.microsoft.com/azure-messaging/eventhubs-emulator` (it needs an Azurite container for blob/metadata
> storage + a config JSON; AMQP on port 5672). The C# project uses it in docker mode
> (`docker/docker-compose-eventhub.yml` + `docker/eventhub-emulator-config.json`) and an in-process
> `UseInMemoryEventHub` in memory mode. So Equipment_Alerts **is** locally verifiable end-to-end; the
> earlier "no emulator, handler-only" note was my mistake and is being fixed by wiring the emulator via
> Testcontainers (Azurite + eventhubs-emulator) with a real EventHub producer + EventProcessorClient
> consumer. The C# emulator config: namespace `emulatorNs1`, hub `breakfast-equipment-alerts`,
> 2 partitions; connection string
> `Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;`.

## 1. EventGrid_Webhook → `ingredientShipments` (DONE — implemented + verified)

**C# behaviour:** `POST /reporting/eventgrid` (see `Endpoints.EventGridWebhook`) receives an EventGrid
webhook payload (array of events) describing an ingredient delivery; the SUT ingests an
`IngredientShipment` reporting row; the GraphQL `ingredientShipments` query returns it.

**Java design:**
- `EventGridWebhookController` `@PostMapping("/reporting/eventgrid")` accepting `List<Map<String,Object>>`
  (the EventGrid envelope), handling the subscription-validation handshake event and the
  ingredient-delivery event.
- `IngredientShipmentEntity` (JPA) + repository in the reporting store.
- `ingredientShipments` `@QueryMapping` + a `IngredientShipment` type in `schema.graphqls`.

**Verification (local, docker mode):** a component test posts an EventGrid payload to the webhook, asserts
2xx, then queries `{ ingredientShipments { ... } }` and asserts the delivery appears. Add across all four
frameworks.

## 2. Batch_Completions → `batchCompletions` (VERIFIABLE locally — Pub/Sub emulator)

**C# behaviour:** creating a pancake/waffle batch publishes a batch-completion event to Pub/Sub; a
consumer ingests a `BatchCompletionRecord`; the GraphQL `batchCompletions` query returns it.

**Java state:** the SUT already publishes `PancakeBatchCompletedEvent` / `WaffleBatchCompletedEvent`
(currently via `LoggingPubSubPublisher`). To complete this feature:
- Publish those events to the real Pub/Sub topic (the Pub/Sub emulator is already wired for customer
  feedback, so this is consistent and verifiable).
- `BatchCompletionConsumer` (Pub/Sub subscriber, like `CustomerFeedbackConsumer`) → `BatchCompletionRecord`
  JPA store.
- `batchCompletions` `@QueryMapping` + schema type.

**Verification (local, docker mode):** create a batch, await the consumer, query `{ batchCompletions }`
and assert the batch appears (Awaitility, like the Orders outbox test). Add across all four frameworks.

## 3. Equipment_Alerts → `equipmentAlerts` (NOT locally verifiable — needs an Event Hubs emulator)

**C# behaviour:** creating a batch publishes an `EquipmentAlertEvent` to **Azure Event Hubs**; an Event
Hubs consumer ingests an `EquipmentAlert`; the GraphQL `equipmentAlerts` query returns it.

**Java state:** the SUT publishes `EquipmentAlertEvent` via `LoggingEventHubPublisher`. The blocker is that
**Azure Event Hubs has no maintained local emulator** (noted in the original plan's Risks). So while the
SUT-side consumer + store + query can be built, the end-to-end flow cannot be driven by the in-process
docker-mode suite the way Cosmos/Kafka/Pub-Sub are.

**Design (build the SUT side, defer the e2e test):**
- `EquipmentAlertEntity` + repository; `equipmentAlerts` `@QueryMapping` + schema type.
- An Event Hubs consumer (`azure-messaging-eventhubs` processor) writing alerts to the store.

**Verification plan (cannot run in the local docker suite):**
- Option A (recommended): unit-test the ingestion path directly — invoke the consumer's handler with a
  synthetic `EquipmentAlertEvent` and assert an `EquipmentAlert` row is written and surfaced by the query.
  This verifies everything except the Event Hubs transport.
- Option B: run the newer Azure Event Hubs emulator container (preview) in a dedicated profile and wire a
  Testcontainers `GenericContainer` for it; gate the e2e scenario behind that profile.
- Option C: in `external-sut` mode against a real Azure Event Hubs namespace, run the full e2e scenario.

## Summary

| Feature | SUT build | Local e2e test | Status |
|---|---|---|---|
| EventGrid_Webhook / `ingredientShipments` | controller + entity + query | yes (POST + GraphQL) | **DONE** |
| Batch_Completions / `batchCompletions` | Pub/Sub publisher + consumer + entity + query | yes (Pub/Sub emulator) | **DONE** |
| Equipment_Alerts / `equipmentAlerts` | consumer handler + entity + query | handler+store+query yes; Event Hubs transport only in external-sut/Azure | **DONE** (transport caveat) |

All scenario/feature parity items are now implemented and green across all four frameworks. The single
residual is that the Azure Event Hubs *transport* for Equipment_Alerts is not exercised by the local
docker suite (no emulator) — only its ingestion handler/store/query are, with the transport verified in
external-sut/Azure.
