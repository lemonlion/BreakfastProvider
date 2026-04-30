# Copilot Instructions — Platform.Templates.ComponentTests.BreakfastProvider

## Project Overview

This is the **Breakfast Provider** platform service, owned by **Team Griddle**. It provides breakfast preparation capabilities including pancake/waffle creation, order management, topping customisation, ingredient sourcing from downstream dairy and supplier services, menu management, and recipe logging.

## Tech Stack

- **.NET 10** (C#) — all projects target `net10.0`
- **ASP.NET Core Web API** with MVC controllers (not Minimal APIs)
- **In-process ASP.NET Core fakes** for downstream service simulation in component tests
- **Serilog** for structured logging
- **OpenTelemetry** for distributed traces, metrics, and log correlation (OTLP exporter)
- **prometheus-net** for Prometheus metrics exposition (`/metrics` endpoint), ASP.NET Core HTTP metrics, HttpClient metrics, and health check status metrics
- **FluentValidation** for request validation
- **System.Text.Json** for serialisation
- **Microsoft.AspNetCore.OpenApi** + **Scalar** for OpenAPI documentation
- **Bielu.AspNetCore.AsyncApi** (backed by **ByteBard.AsyncAPI.NET**) for AsyncAPI documentation
- **HotChocolate** for GraphQL reporting endpoints (business intelligence queries)
- **Entity Framework Core** with SQL Server (Docker/production) and SQLite (in-memory tests) for the reporting database

## Architecture

The solution follows a straightforward **API + Tests** layout with feature-based organisation:

| Layer | Project | Responsibility |
|---|---|---|
| API | `BreakfastProvider.Api` | Controllers, request/response models, DI composition root |
| Tests | `BreakfastProvider.Tests.Component` | LightBDD component tests, fakes, infrastructure |

Feature areas are organised by endpoint concern (e.g. `Pancakes/`, `Waffles/`, `Orders/`, `Toppings/`, `Menu/`, `Ingredients/`, `DailySpecials/`, `Reporting/`).

## Code Patterns & Conventions

### Controllers

- Inherit from `ControllerBase`
- Decorate with `[ApiController]`, `[Route("...")]`, `[Produces]`, `[Consumes]`
- Inject services and `HttpClient` via constructor
- Validate using FluentValidation validators exclusively (no data annotations)

### Domain Models

- **Request models** are validated via FluentValidation validators in the `Validators/` folder (e.g. `PancakeRequestValidator`, `OrderRequestValidator`)
- **Response models** use simple record/class structures (e.g. `PancakeResponse` with `BatchId` and `Ingredients`)
- Downstream responses mapped to internal models (e.g. `MilkResponse` from Cow Service)

### Downstream Services

| Service | Purpose | Endpoint |
|---|---|---|
| **Cow Service** | Provides cow's milk for standard recipes | `GET /milk` |
| **Goat Service** | Provides goat milk for specialty items | `GET /goat-milk` |
| **Supplier Service** | Checks ingredient availability and sourcing | `GET /ingredients/{name}/availability` |
| **Kitchen Service** | Handles cooking preparation and timing | `POST /prepare`, `GET /status/{orderId}` |

### Configuration

- **Cosmos DB** for storage (orders, recipes, audit logs, outbox messages); transactional outbox uses `TransactionalBatch` for atomic writes
- Strongly-typed options via `IOptions<T>` / `IOptionsMonitor<T>`
- Feature-specific config classes use `{Feature}Config` suffix (e.g. `PancakeConfig`, `OrderConfig`, `ToppingConfig`)
- `BaseConfig` base class for external service configs (`BaseAddress`)

### Dependency Injection

- Layer registration via extension methods: `services.AddBreakfastProviders(configuration)`
- Feature-specific registration: `AddPancakeHandlers()`, `AddOrderHandlers()`, `AddToppingValidators()`, etc.

### Naming Conventions

- **Namespaces:** `BreakfastProvider.Api.{Feature}`
- **Handlers:** `{Action}{Entity}Handler` (e.g. `CreatePancakeHandler`, `GetOrderHandler`)
- **Validators:** `{Request}Validator` using FluentValidation
- **Config:** `{Feature}Config` (e.g. `PancakeConfig`, `OrderConfig`)

### C# Style

- Use **nullable reference types** (enabled globally)
- Use **implicit usings** (enabled globally)
- Use **file-scoped namespaces**
- **Primary constructors** are acceptable for new code
- Prefer **string interpolation** (`$"..."`) over string concatenation
- Prefer `record` types for DTOs and requests where appropriate
- `[Description]` attributes from `System.ComponentModel` are used on Kafka event models and message headers for **AsyncAPI documentation generation** — these are not data-annotation validators
- `[ConfigurationKeyName]` in `ProgramSettings` maps Azure App Service environment variables to C# properties — this is necessary infrastructure, not a data-annotation validator

### Observability (OpenTelemetry)

- **`Telemetry/DiagnosticsConfig.cs`** defines the shared `ActivitySource` and `Meter` (service name: `BreakfastProvider.Api`) plus static metric instruments (counters, histograms)
- **Traces**: Custom `Activity` spans in `OrderService`, `RecipeLogger`, `OutboxProcessor`, and `KafkaEventPublisher`; ASP.NET Core and HttpClient auto-instrumentation for inbound/outbound HTTP
- **Metrics**: `breakfast.orders.created`, `breakfast.orders.status_changed`, `breakfast.recipes.logged`, `breakfast.outbox.messages_dispatched`, `breakfast.outbox.messages_failed` (Counters); `breakfast.cache.hits`, `breakfast.cache.misses` (Counters with `cache.name` tag); `breakfast.kafka.publish.duration` (Histogram)
- **Logs**: Serilog for structured console output alongside OpenTelemetry log bridge (`builder.Logging.AddOpenTelemetry()`) for trace-log correlation
- **Exporters**: OTLP exporter for traces, metrics, and logs (configured via standard `OTEL_EXPORTER_OTLP_ENDPOINT` env var)
- **Correlation**: `CorrelationIdMiddleware` enriches the current `Activity` with `correlation.id` tag
- When adding new service methods, create an `Activity` via `DiagnosticsConfig.ActivitySource.StartActivity("ClassName.MethodName")` and record relevant metrics via the static instruments in `DiagnosticsConfig`

### Prometheus

- **prometheus-net.AspNetCore** exposes metrics at `GET /metrics` via `app.MapMetrics()` and collects HTTP request metrics via `app.UseHttpMetrics()`
- **prometheus-net.AspNetCore.HealthChecks** forwards ASP.NET Core health check status to Prometheus via `.ForwardToPrometheus()`
- **IHttpClientFactory metrics** enabled via `services.UseHttpClientMetrics()` — tracks request count, duration, and in-progress for all named HttpClients
- **.NET Meters adapter** automatically publishes all `System.Diagnostics.Metrics` instruments (including `DiagnosticsConfig.Meter`) as Prometheus metrics
- **Exemplars** are automatically attached from `Activity.Current` (trace_id, span_id) when scraped by an OpenMetrics-capable client
- **Docker**: Prometheus server runs in `docker-compose-prometheus.yml` on port `9090`, scraping the SUT at `breakfast-provider-api.local:8080/metrics`

### Jaeger

- **Jaeger v2** (all-in-one) runs in `docker-compose-jaeger.yml` for distributed trace visualisation
- Accepts traces via **OTLP** on port `4317` (gRPC) and `4318` (HTTP) — no additional SDK packages required
- In Docker SUT mode, `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger.local:4317` is set in `docker-compose-sut.yml`
- For local (non-Docker) development, set `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`
- Jaeger UI accessible at `http://localhost:16686`
- Also provisioned as a Grafana datasource for cross-referencing traces with metrics

## Testing

Comprehensive testing conventions, patterns, and infrastructure are documented in the Claude skill at `.claude/skills/component-tests/`. See [README.md](.claude/skills/component-tests/README.md) for the full index.

| File | Read this when… |
|---|---|
| **SKILL.md** | Starting any component test work — core rules, hard constraints, TDD workflow |
| **naming-conventions.md** | Naming a feature class, scenario, or step method |
| **composite-patterns.md** | Writing or refactoring CompositeStep logic, config-as-steps, grouped assertions |
| **assertion-patterns.md** | Adding response, ingredient, topping, or downstream assertions |
| **test-infrastructure.md** | Working with test modes, fake services, or report generation |

## API Conventions

- Route prefix: `/` (e.g. `/pancakes`, `/waffles`, `/orders`, `/eggs`, `/milk`, `/flour`, `/toppings`, `/menu`, `/daily-specials`, `/health`, `/graphql`)
- Swagger/OpenAPI via Swashbuckle
- Standard REST conventions: POST for creation, GET for retrieval, PATCH for updates, DELETE for removal
- Validation returns 400 Bad Request with ProblemDetails
- Downstream errors return 502 Bad Gateway with ProblemDetails
- `X-Correlation-Id` header propagated on all responses via `CorrelationIdMiddleware` and to downstream services via `CorrelationIdDelegatingHandler`
- Feature flags gate endpoint availability (e.g. `IsGoatMilkEnabled`, `IsRaspberryToppingEnabled`)
- Menu responses are cached via `IMemoryCache` (5-minute TTL); cache clearable via `DELETE /menu/cache`
- Order status transitions follow a state machine: Created → Preparing → Ready → Completed, or Created → Cancelled
- Order creation is rate-limited via ASP.NET Core `[EnableRateLimiting]` with a configurable fixed window policy (`RateLimitConfig`)
- Order item count is capped by `OrderConfig.MaxItemsPerOrder` (cross-field validation)
- `GET /orders` returns paginated results via `PaginatedResponse<T>` with `page` and `pageSize` query parameters
- `PUT /toppings/{id}` supports updating existing toppings with full validation (XSS protection, required fields)
- XSS protection via FluentValidation on user-input fields (e.g. `ToppingRequestValidator`, `UpdateToppingRequestValidator`)
- Daily specials support `Idempotency-Key` header on `POST /daily-specials/orders` for idempotent order creation
- Daily specials enforce per-special order limits via `DailySpecialsConfig.MaxOrdersPerSpecial`; returns 409 Conflict when sold out
- **Health checks** conform to ASP.NET Core health check standards with custom JSON response writer:
  - Endpoint: `GET /health` returns JSON with overall status and per-dependency entries
  - Downstream service checks: Cow, Goat, Supplier, Kitchen (tagged `downstream`, `api`; failure → `Degraded`)
  - Infrastructure checks: Cosmos DB (tagged `infrastructure`, `database`), Kafka (tagged `infrastructure`, `messaging`)
  - Custom `IHealthCheck` implementations in `Services/HealthChecks/`
  - `NoOpHealthCheck` supports both `Healthy` (string description) and custom `HealthCheckResult` constructors; production code returns `Unhealthy` when a dependency is genuinely missing
  - Test infrastructure replaces CosmosDb health check with no-op via `ReplaceCosmosDbHealthCheckWithNoOp()` when `RunWithAnInMemoryDatabase` is true
  - Test infrastructure replaces Kafka health check with no-op via `ReplaceKafkaHealthCheckWithNoOp()` in in-memory mode
- Each fake service exposes a `GET /health` endpoint for downstream health probing

## Infrastructure & Local Development

- **Fake services** under `fakes/` are standalone **ASP.NET Core Minimal API** projects that mimic downstream dependencies (Cow Service, Goat Service, Supplier Service, Kitchen Service)
- During component tests, fakes are spun up **in-process** using `WebApplicationFactory<TProgram>` via `InMemoryFakeHelper.Create<TProgram>(url)`, managed by `ConfiguredLightBddScopeAttribute` global setup/teardown
- Feature flags in `ComponentTestSettings` control which fakes run in-memory (e.g. `RunWithAnInMemoryCowService`, `RunWithAnInMemoryGoatService`, `RunWithAnInMemorySupplierService`, `RunWithAnInMemoryKitchenService`)
- **External SUT mode** (`docker-compose-sut.yml`) runs the BreakfastProvider API itself in a Docker container alongside all dependencies. Catches Docker packaging and config issues before Dev & Staging. Uses post-deployment mode settings (`RunAgainstExternalServiceUnderTest: true`, `ExternalServiceUnderTestUrl: http://localhost:5080`). API exposed on port `5080`. Convenience scripts: `docker/docker-compose-external-sut-up.bat` (or `.sh`). CI job: `component-tests-external-sut` in `ci-pull-request.yml` with `test-run-type: external-sut`.
- **Manual Docker external SUT mode** (`switch-to-manual-docker-external-sut.bat` / `.sh`) sets `RunAgainstExternalServiceUnderTest: true` with `EnableDockerInSetupAndTearDown: false`, allowing developers to start Docker containers manually (via `docker-compose-external-sut-up.bat`) and run tests without the framework managing container lifecycle. Use this when you want containers to survive across multiple test runs.
- **Post-deployment mode** (`RunAgainstExternalServiceUnderTest: true`) targets a fully deployed external API — no in-process services, no fakes, no direct infrastructure access. Infrastructure-dependent steps are bypassed via `[SkipStepIf]`; scenarios whose primary assertions require unavailable infrastructure are ignored via `[IgnoreIf]`. Skip reasons are constants in `IgnoreReasons.cs`.
- **Automatic Docker lifecycle** (`EnableDockerInSetupAndTearDown: true`) uses the Docker Compose CLI to automatically start/stop Docker Compose during test setup/teardown via `DockerComposeOrchestrator`. When combined with `RunAgainstExternalServiceUnderTest: true`, includes the SUT compose file. `SkipDockerTearDown: true` (default in Docker mode) leaves containers running after teardown so the next run can reuse warm containers — the Cosmos DB emulator is unstable when recycled via cold restarts.
- **Prometheus** (`docker-compose-prometheus.yml`) runs alongside all Docker modes, scraping the SUT `/metrics` endpoint every 10 seconds. Accessible at `http://localhost:9090`.
- **Grafana** (`docker-compose-grafana.yml`) runs alongside all Docker modes with auto-provisioned Prometheus and Jaeger datasources and Breakfast Provider dashboard. Anonymous admin auth, no login required. Accessible at `http://localhost:3000`.
- **Jaeger** (`docker-compose-jaeger.yml`) runs alongside all Docker modes, collecting distributed traces via OTLP. Jaeger UI accessible at `http://localhost:16686`.
- Generated documentation outputs:
  - `ComponentSpecificationsWithExamples.html` — with PlantUML interaction diagrams (for DevPortal)
  - `ComponentSpecifications.yml` — plain YAML spec (source-controlled in `/docs/`)
  - `FeaturesReport.html` — full report with test run details

### Convenience Scripts

All Docker convenience scripts in `docker/` have both `.bat` (Windows) and `.sh` (Linux/macOS) equivalents:
- `docker-compose-up.bat` / `.sh` — Start all dependencies
- `docker-compose-external-sut-up.bat` / `.sh` — Start all dependencies + SUT
- `docker-compose-database-up.bat` / `.sh` — Start only the database

Test mode switch scripts in `tests/BreakfastProvider.Tests.Component/Configure/` also have both `.bat` and `.sh` equivalents:
- `switch-to-inmemory` — In-memory mode (default)
- `switch-to-docker` — Docker mode (auto-managed containers)
- `switch-to-docker-external-sut` — External SUT (auto-managed containers)
- `switch-to-manual-docker-external-sut` — External SUT (manually managed containers)
