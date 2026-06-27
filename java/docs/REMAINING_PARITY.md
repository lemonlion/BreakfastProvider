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

## `Order_Summaries_Should_Return_An_Empty_List_When_No_Orders_Exist` — now a full component test

This C# scenario asserts the `orderSummaries` GraphQL query returns an **empty** list when no orders exist.
It can't share the docker suite's reporting store (the shared MSSQL Testcontainer accumulates orders across
the whole JVM, so `order_summaries` is never empty mid-suite). It is now reproduced as a real component
test in **all four frameworks** via an isolated store:

- `EmptyReportingBackendsInitializer` runs the normal `BackendsInitializer` then overrides only the
  relational datasource to a fresh in-memory **H2** (`jdbc:h2:mem:empty-reporting`, `ddl-auto=create-drop`)
  and sets `breakfast.background-consumers.enabled=false`. The context creates no orders, so the query
  genuinely returns `[]`.
- The `breakfast.background-consumers.enabled` flag (prod-default ON via `matchIfMissing=true`) gates the
  six background consumers (BatchCompletion/CustomerFeedback/EventHub processor + the two Kafka listeners +
  OutboxProcessor). The isolated query-only context turns them OFF so it doesn't join the shared Kafka
  groups / Pub-Sub subscriptions / Event Hubs group and steal messages from the main context's reporting
  tests.
- Tests: `ReportingEmptyComponentTest` (JUnit5), `ReportingEmptyTestNgTest` (TestNG), `ReportingEmptySpec`
  (Spock), and the isolated Cucumber suite `RunCucumberEmptyReportingTest` (glue + `features-emptyreporting`).
  `ReportingResolverContractTest` (plain no-Spring) additionally pins the resolver's empty-collection
  contract.

**Reporting is therefore 8/8 in every framework** — every C# domain now has Java scenario count ≥ C# in all
four frameworks, with no residual scenario.

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
