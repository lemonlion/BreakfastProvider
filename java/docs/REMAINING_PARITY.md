# Parity status — Java twin vs C# BreakfastProvider

The Java twin mirrors the C# component-test scenario inventory **per domain, across all four frameworks**
(JUnit 5, TestNG, Cucumber, Spock). The C# canonical inventory is
`tests/BreakfastProvider.Tests.Component.LightBDD/Scenarios`.

## Per-domain scenario parity (Java JUnit 5 vs C# LightBDD)

| Domain | C# | Java | Notes |
|---|---|---|---|
| Orders | 20 | 20 | incl. outbox-processed + outbox-failed-after-retries; rate-limit lives in the Overrides context |
| Infrastructure | 11 | 11 | health/correlation/telemetry; forward-to-supplier clears the `/menu` cache first |
| Toppings | 10 | 10 | update/delete on a seeded id (service is stateless over its seed); raspberry-flag in Overrides |
| Ingredients (→ MilkSourcing) | 8 | 8 | incl. cow-timeout → 502; goat-milk-disabled flag in Overrides |
| Reporting | 8 | **7** | see "Single residual" below |
| ChefNotes | 8 | 8 | |
| DailySpecials | 8 | 8 | |
| Grpc | 7 | 7 | |
| Feedback / IngredientWaste / Inventory / RecipeReviews | 6 | 6 | |
| AuditLogs / CustomerPreferences / IngredientUsage / Reservations | 5 | 5 | |
| Pancakes / Waffles / Staff | 4 | 4 | |
| Menu / Specifications / AppleCinnamonMuffins (→ Muffins) | 3 | 3 | |
| RecipeCosts / CustomerFeedback | 1 | 1 | |

Configuration-override scenarios (C# recreates the app with different config) live in one consolidated
extra Spring context per framework — `OverridesComponentTest` / `…TestNgTest` / `…Spec` and the Cucumber
`features-ratelimit` suite — covering Rate_Limiting (permit-limit=1), Toppings Feature_Flag (raspberry off)
and Ingredients Goat_Milk_Feature_Flag (off). Kept in one context to bound the number of heavyweight
backend-bearing Spring contexts per JVM.

All Reporting event-ingestion channels are implemented and verified end-to-end in the local docker suite:
`orderSummaries`, `popularRecipes`, `ingredientShipments` (EventGrid webhook), `batchCompletions`
(real Pub/Sub publish + consumer), `equipmentAlerts` (real Azure Event Hubs via the
`mcr.microsoft.com/azure-messaging/eventhubs-emulator` + Azurite, Testcontainers), and `recipeReports` +
`ingredientUsage` (recipe-log Kafka consumer → reporting projection).

## Single residual — `Order_Summaries_Should_Return_An_Empty_List_When_No_Orders_Exist`

This is the only C# scenario not reproduced as a local component test. It asserts the `orderSummaries`
GraphQL query returns an **empty** list when no orders exist.

**Why it can't run in the shared docker suite:** the reporting store is the shared MSSQL Testcontainer,
and every framework module creates orders across its scenarios in one JVM, so `order_summaries` is never
empty when the test would run. Emptiness can't be asserted without isolating or truncating the store,
which would corrupt the other scenarios' data.

**Verification plan (pick one):**
- **Isolated context (recommended):** a dedicated `@SpringBootTest` context with its own empty reporting
  schema (e.g. a per-test H2 datasource bound only for this test, or `@Sql` truncation of `order_summaries`
  in a `@DirtiesContext` context) that creates no orders, then asserts `{ orderSummaries }` is `[]`.
- **external-sut lane:** against a freshly-provisioned reporting database (empty at start), assert the
  query returns `[]` before any order is created.
- **Contract unit test:** call the `ReportingGraphQlController.orderSummaries()` resolver with an empty
  `OrderSummaryRepository` (mock/empty) and assert it returns an empty list (verifies the resolver's
  empty-collection contract, minus the GraphQL transport).

## Push-to-verify items (cannot run on this workstation)

- **GitHub Actions CI lanes** (`_tests-java.yml` + the `java-*` jobs in `ci-main.yml`) and the GitHub
  Pages publication of the four Java framework reports are verified only by pushing the branch and
  observing the Actions run + the published site. They are wired but not exercised locally.
- **external-sut run mode:** the non-`@SpringBootTest` external-SUT bases (driving a separately-started
  SUT over HTTP/gRPC) are a run-mode variant; they are exercised in CI/external environments, not the
  local docker suite which uses in-process `@SpringBootTest` + Testcontainers.

## Resilience notes (added for suite stability)

- `CosmosRetry`: bounded retry (503/408/449 + "Connection refused") around all Cosmos data-plane ops
  (repository, outbox writer/store, idempotency store). The emulator gateway intermittently refuses
  connections under the full suite's write load; production Cosmos likewise expects throttling retries.
- Cosmos `buildClient()` is retried (eager DatabaseAccount read can 408 at reactor-tail saturation).
- `recipe-logs` / `recipe-cost-calculated` Kafka topics are pre-declared as `NewTopic` beans so consumers
  bind at startup (no metadata-discovery lag on the first message).
- The milk-sourcing `RestTemplate` has a 2s read timeout so a slow cow downstream surfaces as 502.
