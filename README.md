# Breakfast Provider<a name="top"></a>

Provides breakfast preparation capabilities including pancake/waffle creation, order management, topping customisation, ingredient sourcing from downstream dairy and supplier services, menu management, and recipe logging.

## Purpose [↑](#top)<a name="purpose"></a>

This project is a **deliberately opinionated reference implementation** of a component testing design philosophy. It exists to demonstrate how a modern .NET service can be built and tested, serving as a template for teams adopting outside-in component testing.

## Table Of Contents<a name="table-of-contents"></a>

* [Purpose](#purpose)
* [Testing Philosophy](#testing-philosophy)
  + [Component Test First](#component-test-first)
  + [Test At The External Contract Layer](#test-at-the-external-contract-layer)
  + [Test The Service's Effect On The World](#test-the-services-effect-on-the-world)
  + [When Are Unit Tests Justified?](#when-are-unit-tests-justified)
  + [Configurable Test Modes](#configurable-test-modes)
  + [Fail Early, Fail Fast](#fail-early-fail-fast)
  + [Autogenerate Plain English Specifications](#autogenerate-plain-english-specifications)
  + [Source Controlled Specifications](#source-controlled-specifications)
  + [Autogenerate Sequence Diagrams](#autogenerate-sequence-diagrams)
  + [Automate Safety As Much As Possible](#automate-safety-as-much-as-possible)
  + [Tests Should Just Work On Pull And Build](#tests-should-just-work-on-pull-and-build)
* [Mocking Strategy](#mocking-strategy)
  + [Mock At The Lowest Level](#mock-at-the-lowest-level)
  + [HTTP Mocks Are Real Services](#http-mocks-are-real-services)
  + [Same Mocks Everywhere](#same-mocks-everywhere)
  + [Control Mocks Through Request Headers](#control-mocks-through-request-headers)
  + [Mock At The External Boundary In Deployed Environments](#mock-at-the-external-boundary-in-deployed-environments)
  + [Use Real Services When Possible](#use-real-services-when-possible)
* [Getting Started](#getting-started)
  + [Prerequisites](#prerequisites)
  + [Build](#build)
  + [Run the API](#run-the-api)
* [Running the Component Tests](#running-the-component-tests)
  + [Quick Switch Scripts](#quick-switch-scripts)
  + [In Memory (Default)](#in-memory)
  + [Docker Prerequisites](#docker-prerequisites)
  + [With Docker Dependencies](#with-docker-database--dependencies)
  + [With External SUT in Docker](#with-external-sut-in-docker)
  + [Run Unit Tests](#run-unit-tests)
* [Component Test Configurations](#component-test-configurations)
  + [In-Memory Mode](#in-memory-mode)
  + [Dependencies In-Docker Mode](#dependencies-in-docker-mode)
  + [Fully In-Docker Mode (External SUT)](#fully-in-docker-mode)
  + [Post-Deployment Mode](#post-deployment-mode)
  + [Mode Comparison](#mode-comparison)
* [Project Structure](#project-structure)
* [Tech Stack](#tech-stack)
* [API Endpoints](#api-endpoints)
* [Downstream Services](#downstream-services)
* [Configuration](#configuration)
* [Logging](#logging)
* [Observability](#observability)
  + [OpenTelemetry](#opentelemetry)
  + [Prometheus](#prometheus)
  + [Jaeger](#jaeger)
  + [Grafana](#grafana)
* [Specifications & Documentation](#specifications--documentation)
* [Build and Deploy](#build-and-deploy)
* [Docker Folder](#docker-folder)
  + [Docker Compose Files](#docker-compose-files)
    - [docker-compose-database.yml](#docker-compose-databaseyml)
    - [docker-compose-storage.yml](#docker-compose-storageyml)
    - [docker-compose-fakes.yml](#docker-compose-fakesyml)
    - [docker-compose-messaging.yml](#docker-compose-messagingyml)
    - [docker-compose-prometheus.yml](#docker-compose-prometheusyml)
    - [docker-compose-grafana.yml](#docker-compose-grafanayml)
    - [docker-compose-jaeger.yml](#docker-compose-jaegeryml)
    - [docker-compose-sut.yml](#docker-compose-sutyml)
  + [Convenience Scripts](#convenience-scripts)
  + [kafka-config](#kafka-config)
    * [Configuration Files](#configuration-files)
    * [Keystore files](#keystore-files)
    * [Certificate files](#certificate-files)
    * [Generating certificates and keystore files](#generating-certificates-and-keystore-files)
    * [Cleanup: Deleting old certificates](#cleanup-deleting-old-certificates)
    - [Scripts](#scripts)
    - [Linux Scripts](#linux-scripts)
* [Fakes](#fakes)
* [Outbox Pattern](#outbox-pattern)
* [Kafka Events](#kafka-events)
* [Resources](#resources)

## Testing Philosophy [↑](#top)<a name="testing-philosophy"></a>

This project embodies a specific testing philosophy. It is deliberately opinionated because it serves as a reference implementation of that philosophy. The principles below should be followed when contributing to this project, and serve as a guide for teams adopting this approach in their own services.

### Component Test First [↑](#top)<a name="component-test-first"></a>

**95% of the time or more, component tests are preferable to unit tests.** Component tests verify that the service achieves its goal _behaviour_, regardless of the specific implementation. Unit tests lock you into an implementation, a design, and a technology — and they obfuscate the required business behaviour.

With modern in-process `WebApplicationFactory`, component tests execute in roughly **~50ms per scenario** — fast enough that the traditional speed argument for unit tests is now moot in the large majority of situations.

Component tests also catch entire categories of bugs that unit tests miss:
- A FluentValidation validator that passes unit tests but **isn't registered in DI** — a component test catches it; a unit test doesn't.
- Middleware ordering, serialisation quirks, content negotiation — all exercised through the HTTP layer.
- The full pipeline — validation wiring, DI, middleware, serialisation, and behaviour — verified in a single assertion.

### Test At The External Contract Layer [↑](#top)<a name="test-at-the-external-contract-layer"></a>

Always test at the **external communication layer** of the service — usually HTTP. This means your tests interact with the service exactly as a real consumer would: sending HTTP requests and asserting on HTTP responses.

This has powerful benefits:
- You can **refactor implementations heavily** without breaking your tests, because tests are coupled to the contract, not the internals.
- The **same tests run across all modes** — in-process with in-memory fake dependencies, or with dependencies in Docker, or with the full service in Docker, or as post-deployment integration tests against a live environment. Write once, run everywhere.
- You can **migrate your service from one technology to another** (e.g. Azure Function to Web API) and your tests still pass, because they only know about the HTTP contract.

### Test The Service's Effect On The World [↑](#top)<a name="test-the-services-effect-on-the-world"></a>

Beyond HTTP responses, verify the **side effects** the service has on the world external to it:

- **Queues / Event Buses / Log Syncs** — Check the service has placed a correct message on them (or the test double's version of them). In this project, EventGrid events are verified via `InMemoryEventGridPublisherStore` and Kafka messages via `ConsumedKafkaMessageStore`.
- **External Services / Mocks** — Check the service has sent correct requests to them. Outbound HTTP requests are captured via `FakeRequestStore` and asserted on for content and headers.
- **Databases** — If you can check the effect via behaviour (e.g. a successful `GET /orders/{id}` after a `POST /orders`), do that — it means your tests aren't coupled to your data layer. Otherwise, check the values in the database directly (e.g. outbox messages, audit logs).

### When Are Unit Tests Justified? [↑](#top)<a name="when-are-unit-tests-justified"></a>

Unit tests are still needed in a small number of cases:

- **Pure utility functions** with no HTTP surface (e.g. string helpers, math utilities)
- **Very large input spaces** (50+ combinations) where HTTP roundtrips add unnecessary time
- **Internal logic** that genuinely cannot be triggered through any API endpoint

Unit tests are **not** justified for:
- FluentValidation validators — test via tabular validation scenarios
- Controller action logic — test through the endpoint
- Middleware behaviour — test via Infrastructure features
- State machine transitions — test via parameterised scenarios
- Service methods called by controllers — test through the controller endpoint

### Configurable Test Modes [↑](#top)<a name="configurable-test-modes"></a>

One component test project is configurable to run as **in-memory tests**, **in-docker tests**, **fully-in-docker tests** (external SUT), and **post-deployment integration tests**. This means you write all your tests once, but run them in multiple ways to provide different levels of assurance at each stage of the lifecycle. See [Component Test Configurations](#component-test-configurations) for full details.

### Fail Early, Fail Fast [↑](#top)<a name="fail-early-fail-fast"></a>

The goal is to surface problems at the **earliest possible point**:

1. First, fail tests **locally in memory** — fastest feedback, no dependencies
2. If they pass, fail tests **locally in Docker** — validates real I/O paths
3. If they pass, fail tests **in CI (in-memory & Docker)** — catches environmental issues
4. If they pass, fail tests **in Dev** (post-deployment mode) — validates real deployment
5. If they pass, fail tests **in Staging** (post-deployment mode) — final gate before production

### Autogenerate Plain English Specifications [↑](#top)<a name="autogenerate-plain-english-specifications"></a>

Tests produce documentation in **plain English** with multiple levels of abstraction. This allows anyone to understand the service's behaviour without reading code or manually maintaining separate documentation.

This is achieved with **LightBDD** and **composite steps** (sub-steps). Top-level step names read like business requirements; technical details are nested inside `CompositeStep` sub-steps. This ensures the reader isn't overwhelmed with information at every level.

### Source Controlled Specifications [↑](#top)<a name="source-controlled-specifications"></a>

Autogenerated specifications (`ComponentSpecifications.yml`) are automatically **source controlled** and included in PRs. The team can review specification changes at PR time — catching mistakes in behaviour, not just code. The YAML specification is deterministic: no GUIDs, timestamps, or instance-specific data.

### Autogenerate Sequence Diagrams [↑](#top)<a name="autogenerate-sequence-diagrams"></a>

Tests generate **full flow diagrams** showing calls to the Service Under Test (SUT) and calls from the SUT to its dependencies. This provides clear sequence diagrams for all flows without manual maintenance. [TestTrackingDiagrams](https://github.com/lemonlion/TestTrackingDiagrams) is used for PlantUML diagram generation, embedded in the HTML specification report.

### Automate Safety As Much As Possible [↑](#top)<a name="automate-safety-as-much-as-possible"></a>

Tests run automatically in CI in every configuration (in-memory, in-docker, external SUT). A large subset runs automatically post-deployment in Dev & Staging environments. No manual testing gates — the pipeline catches regressions at every stage.

### Tests Should Just Work On Pull And Build [↑](#top)<a name="tests-should-just-work-on-pull-and-build"></a>

After cloning the repository and building, running the component tests should **just work** with no additional setup, no internet connection, and no Docker. The default configuration runs everything in-memory with in-process fakes.

```shell
git clone <repo-url>
dotnet build BreakfastProvider.sln
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

## Mocking Strategy [↑](#top)<a name="mocking-strategy"></a>

### Mock At The Lowest Level [↑](#top)<a name="mock-at-the-lowest-level"></a>

Mocking happens as **low level as possible** — e.g. mock the Cosmos container class, not the abstraction layer that calls it. This allows you to test as much of your real running code as possible, as quickly as possible, in the testing cycle.

### HTTP Mocks Are Real Services [↑](#top)<a name="http-mocks-are-real-services"></a>

All HTTP mocks are **real ASP.NET Core services** — standalone Minimal API projects under `fakes/`, not WireMock stubs or in-memory code doubles. These are spun up in-process using `WebApplicationFactory<TProgram>` via `InMemoryFakeHelper.Create<TProgram>(url)`, which creates real Kestrel servers bound to TCP ports.

In-memory code stubs are avoided because they are less realistic in terms of logs and autogenerated flow diagrams. Spinning up real services in-memory is a much better simulation of reality — the HTTP handler chain, serialisation, and network paths are all exercised.

### Same Mocks Everywhere [↑](#top)<a name="same-mocks-everywhere"></a>

Mocks are written **once**, as real ASP.NET Core services in the repository. The same mocks are then used:
- **In-memory locally** — spun up in-process via `WebApplicationFactory`
- **In Docker locally** — containerised from the same source code
- **In CI** — both in-memory and Docker modes
- **Deployed to Dev & Staging** — as lightweight Docker containers or Basic App plan services

This eliminates the "works in tests but not in deployment" class of bugs.

### Control Mocks Through Request Headers [↑](#top)<a name="control-mocks-through-request-headers"></a>

Some error scenarios aren't possible to simulate with a fake that behaves correctly (e.g. 500 errors), so this project uses **request headers** to control mock responses per-request. The test sets a header (e.g. `X-Fake-CowService-Scenario: ServiceUnavailable`), the API propagates it to the fake via `FakeHeaderPropagationHandler`, and the fake reads it to return the appropriate canned response.

This works identically across all modes — in-memory, Docker, and deployed.

### Mock At The External Boundary In Deployed Environments [↑](#top)<a name="mock-at-the-external-boundary-in-deployed-environments"></a>

In Dev & Staging environments, the ideal is to mock only at the **external boundary of the system** — i.e. dependencies not under direct control. In practice, this isn't always feasible (you don't always control your dependency's dependencies), so direct dependency mocks are available as a fallback. The same fake services used locally can be deployed alongside the real service.

### Use Real Services When Possible [↑](#top)<a name="use-real-services-when-possible"></a>

Mocks are not always used in Dev & Staging. When real downstream dependencies are available and stable, tests run against them. The mocks exist as a safety net for when real dependencies are unavailable, unstable, or when you need to test specific error scenarios.

## Getting Started [↑](#top)<a name="getting-started"></a>

### Prerequisites [↑](#top)<a name="prerequisites"></a>

- **.NET 10 SDK** — required for all modes
- **(Optional) A container runtime** — required only for Docker modes. Any of the following:
  - [Docker Desktop](https://www.docker.com/products/docker-desktop/) (commercial, free for small teams)
  - [Podman Desktop](https://podman-desktop.io/) (free, open source)
  - [Rancher Desktop](https://rancherdesktop.io/) (free, open source)

> **Note:** For in-memory mode (the default), no container runtime or internet connection is needed. Just clone, build, and run.

### Build [↑](#top)<a name="build"></a>

```shell
dotnet build BreakfastProvider.sln
```

### Run the API [↑](#top)<a name="run-the-api"></a>

```shell
dotnet run --project src/BreakfastProvider.Api/BreakfastProvider.Api.csproj
```

The API will be available at:
- **HTTP:** `http://localhost:5239`
- **HTTPS:** `https://localhost:7270`
- **Swagger UI:** `http://localhost:5239/swagger` (auto-launches in Development)

### Run API and Dependencies in Docker Compose

Ensure the Kafka certificates are generated before running (see [Generating certificates and keystore files](#generating-certificates-and-keystore-files)).

Run `/docker/docker-compose-up.bat` (or `.sh`) to start all dependencies (Cosmos DB emulator, Azurite, EventGrid simulator, Kafka, and fake downstream services).

## Running the Component Tests [↑](#top)<a name="running-the-component-tests"></a>

### Quick Switch Scripts [↑](#top)<a name="quick-switch-scripts"></a>

Use these scripts in `tests/BreakfastProvider.Tests.Component/Configure/` to toggle the test mode in `appsettings.componenttests.json`:

| Script (Windows / Linux) | Mode | Description |
|--------------------------|------|-------------|
| `switch-to-inmemory.bat` / `.sh` | In-memory | All fakes run in-process, no Docker required |
| `switch-to-docker.bat` / `.sh` | Docker | Tests run against real Docker containers (auto-managed) |
| `switch-to-docker-external-sut.bat` / `.sh` | External SUT | API also runs in Docker alongside dependencies (auto-managed) |
| `switch-to-manual-docker-external-sut.bat` / `.sh` | Manual External SUT | Same as External SUT but Docker containers managed manually |

After switching, run the tests normally:

```shell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

### In Memory (Default) [↑](#top)<a name="in-memory"></a>

Nothing special is required. All fakes run in-process by default. No Docker, no internet connection, no setup.

```shell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

### Docker Prerequisites [↑](#top)<a name="docker-prerequisites"></a>

Before running any Docker-based test mode, you need a container runtime installed and running:

| Runtime | Licence | Link |
|---|---|---|
| **Docker Desktop** | Commercial (free for small teams/education) | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) |
| **Podman Desktop** | Free, open source (Apache 2.0) | [podman-desktop.io](https://podman-desktop.io/) |
| **Rancher Desktop** | Free, open source (Apache 2.0) | [rancherdesktop.io](https://rancherdesktop.io/) |

Ensure `docker compose` (v2) is available on your `PATH`. Most desktop runtimes include it by default.

You will also need to generate the Kafka certificates before first use — see [Generating certificates and keystore files](#generating-certificates-and-keystore-files).

### With Docker Dependencies [↑](#top)<a name="with-docker-database--dependencies"></a>

There are two options for running Docker dependencies:

#### Option A: Automatic Docker lifecycle (recommended)

- __Step 1:__ Install and run [Rancher Desktop](https://rancherdesktop.io/), [Podman Desktop](https://podman-desktop.io/), or [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- __Step 2:__ Set `EnableDockerInSetupAndTearDown` to `true` in `appsettings.componenttests.json` and the test framework will automatically start and stop Docker containers via the Docker Compose CLI. No manual Docker commands needed.

Set the `RunWithAnInMemory*` flags to `false` as needed, then run:

```shell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

#### Option B: Manual Docker lifecycle

- __Step 1:__ Install and run [Rancher Desktop](https://rancherdesktop.io/), [Podman Desktop](https://podman-desktop.io/), or [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- __Step 2:__ Run the docker dependencies:
    - Run `/docker/docker-compose-up.bat` (starts all Docker containers: Cosmos DB, Azurite, EventGrid, Kafka, fake services).
    - Alternatively, run individual compose files as needed (see [Docker Compose Files](#docker-compose-files)).
- __Step 3:__ Wait for the database to be ready. Even when the previous script indicates containers are ready, the Cosmos DB emulator can take up to 2 minutes to be fully ready. Run `/docker/check-cosmos-emulator-status.sh` in parallel to know when it's ready.
- __Step 4:__ Run the component tests with the appropriate flags set to `false` in `appsettings.componenttests.json`:
    - `RunWithAnInMemoryDatabase = false`
    - `RunWithAnInMemoryCowService = false`
    - `RunWithAnInMemoryGoatService = false`
    - `RunWithAnInMemorySupplierService = false`
    - `RunWithAnInMemoryKitchenService = false`
    - `RunWithAnInMemoryEventGrid = false`
    - `RunWithAnInMemoryKafkaBroker = false`

In order to build Docker images for the fake services and the SUT behind a corporate proxy:
1. Download your organisation's root CA certificate (e.g. `Root_CA.crt`) from your certificate distribution system, and copy it into the `docker` folder. (It's in .gitignore and shouldn't be committed to the repo.) This is only required for the SUT image — the fake services have no NuGet dependencies and build without it.
2. Copy `nuget.config.github` to `nuget.config` (at the root of the repo), and replace the `GITHUB_NUGET_APIKEY` placeholder with your [GitHub Personal Access Token (PAT)](https://github.com/settings/tokens) that has, at least, the `read:packages` scope. (Note that `nuget.config` has been excluded in .gitignore and shouldn't be committed.)

### With external SUT in Docker [↑](#top)<a name="with-external-sut-in-docker"></a>

Runs the BreakfastProvider API itself inside a Docker container alongside all its dependencies. This catches Docker packaging, configuration, and startup issues before code reaches Dev & Staging environments.

#### Option A: Automatic Docker lifecycle (recommended)

- __Step 1:__ Install and run [Rancher Desktop](https://rancherdesktop.io/), [Podman Desktop](https://podman-desktop.io/), or [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- __Step 2:__ Set both `EnableDockerInSetupAndTearDown` and `RunAgainstExternalServiceUnderTest` to `true` in `appsettings.componenttests.json` (the `ExternalServiceUnderTestUrl` defaults to `http://localhost:5080`). The test framework will automatically spin up the full stack including the SUT:

```shell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

#### Option B: Manual Docker lifecycle

- __Step 1:__ Install and run [Rancher Desktop](https://rancherdesktop.io/), [Podman Desktop](https://podman-desktop.io/), or [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- __Step 2:__ If building behind a corporate proxy, ensure `Root_CA.crt` is in the `docker/` folder (see [With docker database & dependencies](#with-docker-database--dependencies) for details).
- __Step 3:__ Run `/docker/docker-compose-external-sut-up.bat` (or `.sh`) to spin up all Docker containers including the API.
- __Step 4:__ Wait for all services to become healthy. The script uses `--wait` with health checks, but the Cosmos DB emulator can take up to 2 minutes.
- __Step 5:__ Switch to manual Docker external SUT mode and run the tests:

```shell
# Switch appsettings to manual Docker external SUT mode
tests/BreakfastProvider.Tests.Component/Configure/switch-to-manual-docker-external-sut.bat

# Run the tests
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

Alternatively, without switching appsettings, pass environment variables directly:

```shell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj \
  -e RunAgainstExternalServiceUnderTest=true \
  -e ExternalServiceUnderTestUrl=http://localhost:5080
```

The API is available at `http://localhost:5080`. In this mode, tests run in post-deployment mode — infrastructure-dependent steps are skipped via `[SkipStepIf]` and scenarios requiring direct infrastructure access are ignored via `[IgnoreIf]`.

> **Important:** Use `switch-to-manual-docker-external-sut` (not `switch-to-docker-external-sut`) when managing Docker manually. The latter sets `EnableDockerInSetupAndTearDown=true`, which causes the test framework to manage container lifecycle and may tear down your manually started containers.

### Run Unit Tests [↑](#top)<a name="run-unit-tests"></a>

```shell
dotnet test tests/BreakfastProvider.Tests.Unit/BreakfastProvider.Tests.Unit.csproj
```

## Component Test Configurations [↑](#top)<a name="component-test-configurations"></a>

The component tests can be configured in several ways. All modes except post-deployment run in CI and can be run locally.

### In-Memory Mode [↑](#top)<a name="in-memory-mode"></a>

- Tests run completely in memory
- Service runs in-process via `WebApplicationFactory<Program>`
- Fake in-memory database (Cosmos fake) is used
- Fake HTTP dependent services are spun up in-memory from ASP.NET API projects via `InMemoryFakeHelper`
- Fake Kafka emulator and EventGrid publisher run in-memory

**Advantages:** Fastest and most convenient. No Docker or internet connection needed.
**Disadvantages:** Not all features can be tested in-memory. In-memory fakes don't provide the same level of assurance as real infrastructure.

### Dependencies In-Docker Mode [↑](#top)<a name="dependencies-in-docker-mode"></a>

- Service Under Test runs in-process via `WebApplicationFactory<Program>`
- Real Cosmos DB emulator, SQL Server, Azurite, Kafka, and EventGrid simulator run in Docker
- Fake HTTP dependent services run in Docker

**Advantages:** Real/realistic databases and messaging used. No internet connection needed.
**Disadvantages:** Slower to set up. Slower to run tests.

### Fully In-Docker Mode (External SUT) [↑](#top)<a name="fully-in-docker-mode"></a>

- Service Under Test runs in Docker
- Real Cosmos DB emulator, SQL Server, Azurite, Kafka, and EventGrid simulator run in Docker
- Fake HTTP dependent services run in Docker

**Advantages:** Real/realistic databases used. Most closely simulates post-deployment tests, but can still be run locally and in CI so you fail earlier. Catches Docker packaging, environment variable configuration, and startup issues before code reaches Dev & Staging.
**Disadvantages:** Unable to easily track dependency calls for autogenerated sequence diagrams (they will only include calls to and from the SUT). Some tests can't be achieved easily in this mode and are excluded.

### Post-Deployment Mode [↑](#top)<a name="post-deployment-mode"></a>

- Service Under Test runs in Azure Dev/Staging environment
- Tests are run in GitHub Actions calling out to the deployed environment
- Dependencies are all actual services running in Azure, either as real services or mocks deployed to Azure

**Advantages:** Most realistic simulation of production. Debugging uses the same tools as production.
**Disadvantages:** Most inconvenient time to fail. Most difficult to debug. Failure happens after CI and merge, so your main branch may be contaminated with incorrect code.

### Mode Comparison [↑](#top)<a name="mode-comparison"></a>

| Capability | In-Memory | Docker | External SUT | Post-Deployment |
|---|---|---|---|---|
| Cow / Goat / Supplier / Kitchen Services | In-process fakes | Docker containers | Docker containers | Real services / deployed mocks |
| EventGrid | In-memory publisher | EventGrid simulator → Azurite | EventGrid simulator → Azurite | Production EventGrid |
| Kafka | In-memory tracked producer | Docker Kafka broker | Docker Kafka broker | Production Kafka |
| Database (Cosmos) | In-memory Cosmos fake | Cosmos DB emulator | Cosmos DB emulator | Production Cosmos DB |
| Database (Reporting) | SQLite (in-memory) | SQL Server (Docker) | SQL Server (Docker) | Production SQL Server |
| API | In-process `WebApplicationFactory` | In-process `WebApplicationFactory` | Docker container (`:5080`) | Deployed service |
| Downstream request inspection | `FakeRequestStore` | `FakeRequestStore` | Unavailable | Unavailable |
| Config overrides | `delayAppCreation` pattern | `delayAppCreation` pattern | Not possible | Not possible |
| Speed | ~20s full suite | ~70s full suite | ~70s + container startup | Depends on network |
| Sequence diagrams | Full (SUT + downstream) | Full (SUT + downstream) | SUT only | SUT only |

## Project Structure [↑](#top)<a name="project-structure"></a>

The solution follows a straightforward **API + Tests** layout with feature-based organisation:

### Source (`src`) [↑](#top)<a name="source-src"></a>

- **BreakfastProvider.Api**
  - Controllers — REST endpoints for pancakes, waffles, orders, toppings, menu, ingredients, milk, audit logs
  - Models — Request/response DTOs and event models
  - Services — Business logic handlers, including `Reporting/` (GraphQL queries, Kafka consumer, EF Core ingester)
  - Storage — Cosmos DB repository layer (operational data); SQL Server via EF Core (reporting database)
  - Events — EventGrid and Kafka event publishing, transactional outbox pattern
  - Validation — FluentValidation validators
  - Configuration — Strongly-typed options classes
  - Filters — ASP.NET Core action filters
  - HttpClients — Typed HTTP clients for downstream services

### Tests (`tests`) [↑](#top)<a name="tests-tests"></a>

- **BreakfastProvider.Tests.Component**
  - Component tests using the standard LightBDD BDD approach
  - In-process fakes for all downstream dependencies
  - SQLite in-memory database for reporting tests (replaces SQL Server in in-memory mode)
  - Infrastructure for test setup/teardown, Kafka consumers, and report generation
- **BreakfastProvider.Tests.Unit**
  - xUnit unit tests for validators and pure logic

### Fakes (`fakes`) [↑](#top)<a name="fakes-fakes"></a>

- **Dependencies.Fakes.CowService** — Fake Cow Service (milk provider)
- **Dependencies.Fakes.GoatService** — Fake Goat Service (goat milk provider)
- **Dependencies.Fakes.SupplierService** — Fake Supplier Service (ingredient availability)
- **Dependencies.Fakes.KitchenService** — Fake Kitchen Service (preparation/cooking)

> Note: The fakes also run in-process during component tests via `WebApplicationFactory<TProgram>`, managed by `ConfiguredLightBddScopeAttribute` global setup/teardown. Feature flags in `appsettings.componenttests.json` control which fakes run in-memory vs Docker.

## Tech Stack [↑](#top)<a name="tech-stack"></a>

- **.NET 10** (C#) — all projects target `net10.0`
- **ASP.NET Core Web API** with MVC controllers (not Minimal APIs)
- **Cosmos DB** for storage (orders, recipes, audit logs, outbox messages)
- **Azure EventGrid** for domain events (e.g. order created), dispatched via the transactional outbox
- **Apache Kafka** for recipe logging events
- **HotChocolate** for GraphQL reporting endpoints (business intelligence queries)
- **Entity Framework Core** with SQL Server (Docker/production) and SQLite (in-memory tests) for the reporting database
- **Serilog** for structured logging
- **OpenTelemetry** for distributed traces, metrics, and log correlation (OTLP exporter)
- **prometheus-net** for Prometheus metrics exposition (`/metrics` endpoint), ASP.NET Core HTTP metrics, HttpClient metrics, and health check status metrics
- **Prometheus** for metrics collection and querying (Docker)
- **Jaeger** for distributed trace visualisation (Docker)
- **Grafana** for metrics dashboards and trace exploration (Docker, auto-provisioned datasources and dashboards)
- **FluentValidation** for request validation
- **System.Text.Json** for serialisation
- **Microsoft.AspNetCore.OpenApi** + **Scalar** for OpenAPI documentation
- **Bielu.AspNetCore.AsyncApi** (backed by **ByteBard.AsyncAPI.NET**) for AsyncAPI documentation
- **LightBDD** for BDD-style component tests
- **xUnit** for unit tests
- **In-process ASP.NET Core fakes** for downstream service simulation in component tests

## API Endpoints [↑](#top)<a name="api-endpoints"></a>

| Method | Path | Description |
|---|---|---|
| `GET` | `/` | Heartbeat — confirms the service is running |
| `GET` | `/health` | Health check endpoint |
| `GET` | `/metrics` | Prometheus metrics endpoint |
| `POST` | `/pancakes` | Create a pancake batch |
| `POST` | `/waffles` | Create a waffle batch |
| `POST` | `/orders` | Create a breakfast order |
| `GET` | `/orders/{id}` | Get order by ID |
| `PATCH` | `/orders/{id}/status` | Update order status (state machine) |
| `GET` | `/milk` | Get milk (proxied from Cow Service) |
| `GET` | `/goat-milk` | Get goat milk (proxied from Goat Service, feature-flagged) |
| `GET` | `/eggs` | Get eggs |
| `GET` | `/flour` | Get flour |
| `GET` | `/toppings` | List available toppings (feature-flag filtered) |
| `POST` | `/toppings` | Add a new topping (XSS validated) |
| `DELETE` | `/toppings/{id}` | Delete a topping |
| `GET` | `/menu` | Get menu items with availability (cached) |
| `DELETE` | `/menu/cache` | Clear menu cache |
| `GET` | `/audit-logs` | Query audit logs (filterable by entityType, entityId) |
| `POST` | `/graphql` | GraphQL endpoint for reporting queries (order summaries, recipe reports, ingredient usage, popular recipes) |

- Swagger/OpenAPI available at `/swagger` in Development
- Validation returns `400 Bad Request` with `ProblemDetails`
- Standard REST conventions: `POST` for creation, `GET` for retrieval, `PATCH` for updates, `DELETE` for removal
- `X-Correlation-Id` header is propagated on all responses
- Downstream service errors return `502 Bad Gateway` with `ProblemDetails`
- Order status transitions follow: `Created → Preparing → Ready → Completed` or `Created → Cancelled`

## Downstream Services [↑](#top)<a name="downstream-services"></a>

| Service | Purpose | Default Local Port | Endpoint |
|---|---|---|---|
| **Cow Service** | Provides cow's milk for standard recipes | `5031` | `GET /milk` |
| **Goat Service** | Provides goat milk for specialty items | `5032` | `GET /goat-milk` |
| **Supplier Service** | Checks ingredient availability and sourcing | `5033` | `GET /ingredients/{name}/availability` |
| **Kitchen Service** | Handles cooking preparation and timing | `5034` | `POST /prepare`, `GET /status/{orderId}` |

## Configuration [↑](#top)<a name="configuration"></a>

Configuration is managed via `appsettings.json` with strongly-typed options using `IOptions<T>` / `IOptionsMonitor<T>`.

| Config Section | Purpose |
|---|---|
| `CosmosConfig` | Cosmos DB connection, database name, timeouts, retries |
| `EventGridConfig` | EventGrid endpoint, topic key, subject, enable/disable |
| `KafkaConfig` | Kafka bootstrap servers, producer/consumer configs, topic names |
| `ToppingRulesConfig` | Business rules (e.g. max toppings per item) |
| `FeatureSwitchesConfig` | Feature flags (e.g. goat milk, raspberry topping) |
| `CowServiceConfig` | Cow Service base address |
| `GoatServiceConfig` | Goat Service base address |
| `SupplierServiceConfig` | Supplier Service base address |
| `KitchenServiceConfig` | Kitchen Service base address |
| `OutboxConfig` | Outbox polling interval, batch size, max retries, enable/disable |
| `ReportingConfig` | SQL Server connection string for the reporting database |

Feature-specific config classes follow the `{Feature}Config` naming convention and inherit from `BaseConfig` (for `BaseAddress`).

## Logging [↑](#top)<a name="logging"></a>

Structured logging is provided by **Serilog** with console output. Configuration is driven by `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

## Observability [↑](#top)<a name="observability"></a>

### OpenTelemetry [↑](#top)<a name="opentelemetry"></a>

Distributed tracing, metrics, and log correlation are provided by **OpenTelemetry** with OTLP exporters:

- **Traces**: Custom `Activity` spans in `OrderService`, `RecipeLogger`, `OutboxProcessor`, and `KafkaEventPublisher`; ASP.NET Core and HttpClient auto-instrumentation for inbound/outbound HTTP
- **Metrics**: Custom counters (`breakfast.orders.created`, `breakfast.orders.status_changed`, `breakfast.recipes.logged`, `breakfast.outbox.messages_dispatched`, `breakfast.outbox.messages_failed`, `breakfast.cache.hits`, `breakfast.cache.misses`) and histograms (`breakfast.kafka.publish.duration`) defined in `Telemetry/DiagnosticsConfig.cs`
- **Logs**: OpenTelemetry log bridge (`builder.Logging.AddOpenTelemetry()`) provides trace-log correlation alongside Serilog
- **Correlation**: `CorrelationIdMiddleware` enriches the current `Activity` with `correlation.id` tag

Configure the OTLP endpoint via the standard `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable.

### Prometheus [↑](#top)<a name="prometheus"></a>

**prometheus-net** exposes metrics at `GET /metrics` for Prometheus scraping:

- **HTTP request metrics** — request count, duration, and in-progress via `app.UseHttpMetrics()`
- **HttpClient metrics** — outbound request tracking via `services.UseHttpClientMetrics()`
- **Health check status** — `aspnetcore_healthcheck_status` gauge via `.ForwardToPrometheus()`
- **.NET Meters adapter** — automatically publishes all `System.Diagnostics.Metrics` instruments (including all custom metrics from `DiagnosticsConfig`) as Prometheus metrics
- **Exemplars** — automatically attached from `Activity.Current` (trace_id, span_id) when scraped by an OpenMetrics-capable client

In Docker mode, a Prometheus server runs via `docker-compose-prometheus.yml` on port `9090`, accessible at `http://localhost:9090`.

### Jaeger [↑](#top)<a name="jaeger"></a>

**Jaeger** provides distributed trace visualisation — search, filter, and inspect full request traces across the API and its downstream dependencies.

- Jaeger v2 runs via `docker-compose-jaeger.yml`, accepting traces via **OTLP** on port `4317` (gRPC) and `4318` (HTTP)
- No additional SDK packages or code changes are needed — the existing OpenTelemetry OTLP exporter sends traces directly to Jaeger
- In Docker SUT mode, `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger.local:4317` is set automatically
- For local development (non-Docker), set `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` to send traces to the Dockerised Jaeger instance
- Jaeger UI accessible at `http://localhost:16686`
- Also available as a Grafana datasource for cross-referencing traces with metrics

### Grafana [↑](#top)<a name="grafana"></a>

A **Grafana** instance is auto-provisioned via `docker-compose-grafana.yml` with:

- **Prometheus datasource** — pre-configured to query `http://prometheus:9090`
- **Jaeger datasource** — pre-configured to query `http://jaeger:16686` for trace exploration
- **Breakfast Provider dashboard** — auto-loaded with panels for all application metrics:
  - HTTP request rate, duration percentiles (p50/p90/p99), in-progress, error rate
  - Orders created, order status transitions, recipes logged
  - Cache hit/miss rate and hit ratio (menu, idempotency)
  - Outbox messages dispatched/failed, Kafka publish duration percentiles
  - Downstream HttpClient request rate and duration by client
  - Health check status per dependency
- **Anonymous admin access** — no login required for local development
- Accessible at `http://localhost:3000`

## Build and Deploy [↑](#top)<a name="build-and-deploy"></a>

### Build

```powershell
dotnet build BreakfastProvider.sln
```

### Run All Tests

```powershell
dotnet test BreakfastProvider.sln
```

## Specifications & Documentation [↑](#top)<a name="specifications--documentation"></a>

After running component tests, the following reports are generated:

| Output | Description |
|---|---|
| `ComponentSpecificationsWithExamples.html` | Specifications with PlantUML interaction diagrams (for DevPortal) |
| `ComponentSpecifications.yml` | Plain YAML spec (source-controlled in `/docs/`) |
| `FeaturesReport.html` | Full report with test run details |

Additional documentation in `/docs/`:

| File | Description |
|---|---|
| `openapi.json` | OpenAPI/Swagger schema |
| `asyncapi.json` | AsyncAPI schema for EventGrid/Kafka events |
| `ComponentSpecifications.yml` | BDD component specifications |

## Docker Folder [↑](#top)<a name="docker-folder"></a>

This folder contains Docker Compose files and supporting configuration for running local dependencies.

### Docker Compose Files [↑](#top)<a name="docker-compose-files"></a>

There are a number of `docker-compose-*.yml` files which can be used to spin up local instances of third-party and cloud dependencies.

#### docker-compose-database.yml [↑](#top)<a name="docker-compose-database-yml"></a>

Spins up local database instances:
- **Azure Cosmos DB Emulator** — Port `8081` (Data Explorer and API endpoint). Configured with 25 partitions, no data persistence between runs.
- **SQL Server 2022** — Port `1433`. Used by the reporting database (`ReportingDbContext` via Entity Framework Core).

#### docker-compose-storage.yml [↑](#top)<a name="docker-compose-storage-yml"></a>

Spins up a local instance of **Azurite** to emulate Azure Blob Storage, Azure Queue Storage, and Azure Table Storage.
- Port `10000` — Blob
- Port `10001` — Queue
- Port `10002` — Table

#### docker-compose-fakes.yml [↑](#top)<a name="docker-compose-fakes-yml"></a>

Spins up Docker containers for the four downstream service fakes. Docker images are automatically built from the fakes' source code — there's no need to build them manually.
- **cow-service** — Port `5031`
- **goat-service** — Port `5032`
- **supplier-service** — Port `5033`
- **kitchen-service** — Port `5034`

#### docker-compose-messaging.yml [↑](#top)<a name="docker-compose-messaging-yml"></a>

Spins up local messaging infrastructure — **EventGrid** and **Kafka**.
- **Azure EventGrid Simulator** — Port `60101`, configured with a topic that subscribes to a storage queue on Azurite
- **Kafka broker** — Port `9092` (SASL_SSL)
- **Kafka UI** — Port `9001` (web interface at `http://localhost:9001`)
- Automatically creates Kafka topics on startup: `breakfast_recipe_logs`, `breakfast_menu_updates`

#### docker-compose-prometheus.yml [↑](#top)<a name="docker-compose-prometheus-yml"></a>

Spins up a **Prometheus** server for scraping application metrics.
- Port `9090` — Prometheus web UI and API
- Scrapes the SUT at `breakfast-provider-api.local:8080/metrics` every 10 seconds
- Configuration file: `docker/prometheus/prometheus.yml`
- Included automatically in all Docker modes (dependency and external SUT)

#### docker-compose-grafana.yml [↑](#top)<a name="docker-compose-grafana-yml"></a>

Spins up a **Grafana** instance with auto-provisioned datasources and dashboards.
- Port `3000` — Grafana web UI at `http://localhost:3000`
- Prometheus datasource auto-provisioned via `docker/grafana/provisioning/datasources/prometheus.yml`
- Jaeger datasource auto-provisioned via `docker/grafana/provisioning/datasources/jaeger.yml`
- Dashboard auto-provisioned from `docker/grafana/dashboards/breakfast-provider.json`
- Anonymous admin auth enabled (no login required)
- Included automatically in all Docker modes

#### docker-compose-jaeger.yml [↑](#top)<a name="docker-compose-jaeger-yml"></a>

Spins up a **Jaeger** instance (v2, all-in-one) for distributed trace collection and visualisation.
- Port `16686` — Jaeger UI at `http://localhost:16686`
- Port `4317` — OTLP gRPC receiver (traces, metrics)
- Port `4318` — OTLP HTTP receiver
- Accepts traces from the existing OpenTelemetry OTLP exporter — no additional SDK packages required
- Included automatically in all Docker modes

#### docker-compose-sut.yml [↑](#top)<a name="docker-compose-sut-yml"></a>

Builds and runs the **BreakfastProvider API** in a Docker container alongside all dependencies. Used for external-SUT component testing and CI validation.
- Port `5080` — API endpoint
- Depends on Cosmos DB (health check), SQL Server (health check), Azurite, Kafka, EventGrid, and all four fake services
- Environment variables override downstream service URLs to Docker service names (e.g. `CowServiceConfig__BaseAddress=http://cow-service:8080`)
- `docker-entrypoint.sh` installs any certificates mounted into `/certs/` (e.g. Kafka CA) before starting the app
- At build time, if `docker/Root_CA.crt` is present it is installed into the SDK container's trust store so `dotnet restore` works behind a corporate proxy (no-op when absent)
- Convenience scripts: `/docker/docker-compose-external-sut-up.bat` (or `.sh`)

### Convenience Scripts [↑](#top)<a name="convenience-scripts"></a>

All Docker convenience scripts in `docker/` have both `.bat` (Windows) and `.sh` (Linux/macOS) equivalents:
- `docker-compose-up.bat` / `.sh` — Start all dependencies
- `docker-compose-external-sut-up.bat` / `.sh` — Start all dependencies + SUT
- `docker-compose-database-up.bat` / `.sh` — Start only the database

Test mode switch scripts in `tests/BreakfastProvider.Tests.Component/Configure/` also have both `.bat` and `.sh` equivalents — see [Quick Switch Scripts](#quick-switch-scripts).

### kafka-config [↑](#top)<a name="kafka-config"></a>

This folder stores configuration files for the Kafka Docker Compose setup, as well as scripts to generate certificates.

##### Configuration Files [↑](#top)<a name="configuration-files"></a>
- `client.properties` — Required by the Kafka container. Contains the username and password along with the security protocol (SASL_SSL).
- `kafka_server_jaas.conf` — Required by the Kafka container for JAAS authentication.

##### Keystore files [↑](#top)<a name="keystore-files"></a>
Required by the Kafka container, generated using `Generate-Certificates.ps1`.
These files **must not** be committed to the repo.
- `certificates/kafka.keystore.jks`
- `certificates/kafka.truststore.jks`

##### Certificate files [↑](#top)<a name="certificate-files"></a>
Required by the local developer's Windows machine, also generated using `Generate-Certificates.ps1`.
These should be installed into your local certificate store using `Install-Certificates.ps1`, and should only be run on first checkout of the repo or when certificates have expired (after one year).
These files **must not** be committed to the repo.
- `certificates/ca.crt`
- `certificates/kafka.crt`

##### Generating certificates and keystore files [↑](#top)<a name="generating-certificates-and-keystore-files"></a>
1. Open a PowerShell prompt with administrative privileges.
2. Run `.\Install-Dependencies.ps1` (unless previously done; safe to re-run).
3. Run `.\Generate-Certificates.ps1`
4. Run `.\Install-Certificates.ps1`
5. Run `.\Update-HostFile.ps1` (unless previously done; safe to re-run).
6. **Do not** commit the contents of the `certificates` folder to the git repo.

##### Cleanup: Deleting old certificates [↑](#top)<a name="cleanup-deleting-old-certificates"></a>
1. Execute `certlm.msc` (Manage Computer Certificates) from the Windows Start Menu.
2. Expand `Trusted Root Certificate Authorities` then `Certificates`.
3. Locate and delete expired `ca.local` certificates.

#### Scripts [↑](#top)<a name="scripts"></a>
- `Install-Dependencies.ps1` — Installs (via `chocolatey`) the tools needed to generate certificate files.
- `Generate-Certificates.ps1` — Uses `openssl` and OpenJDK's `keytool` to generate the required certificates.
- `Install-Certificates.ps1` — Installs generated certificates to the local Windows Certificate Store.
- `Update-HostFile.ps1` — Adds the Kafka Docker hostname to `C:\Windows\System32\drivers\etc\hosts`.

#### Linux Scripts [↑](#top)<a name="linux-scripts"></a>
Used by CI pull request workflows when running component tests with dockerised Kafka:
- `generate-certs.sh` — Generates and installs certificates and keystore files.
- `update-hostfile.sh` — Adds the Kafka Docker hostname to `/etc/hosts`.

> **NOTE**: All config files and scripts hardcode the username and password as `kafka-user` and `kafka-user-secret`, respectively.

## Fakes [↑](#top)<a name="fakes"></a>

The `fakes/` folder contains standalone ASP.NET Core Web API applications used as fakes for downstream service dependencies. Each fake supports scenario-based behaviour via request headers for testing different conditions.

| Fake | Header | Scenarios |
|---|---|---|
| **Cow Service** | `X-Fake-CowService-Scenario` | `ServiceUnavailable` (503), `Timeout` (504) |
| **Goat Service** | `X-Fake-GoatService-Scenario` | `ServiceUnavailable` (503) |
| **Supplier Service** | `X-Fake-SupplierService-Scenario` | `OutOfStock` (200, unavailable), `ServiceUnavailable` (503) |
| **Kitchen Service** | `X-Fake-KitchenService-Scenario` | `KitchenBusy` (503) |

In order to build these Docker images:
1. Copy `nuget.config.github` to `nuget.config` (at the root of the repo), and replace the `GITHUB_NUGET_APIKEY` placeholder with your [GitHub Personal Access Token (PAT)](https://github.com/settings/tokens) that has, at least, the `read:packages` scope. (`nuget.config` is excluded in .gitignore and shouldn't be committed.)

> **Note:** The fake services have no NuGet package dependencies and do not require `Root_CA.crt` for building. Only the SUT image requires it when building behind a corporate proxy.

## Outbox Pattern [↑](#top)<a name="outbox-pattern"></a>

The project uses a **transactional outbox** to guarantee at-least-once delivery of domain events. Instead of publishing directly to EventGrid when an order is created, the `OrderService` writes the order document **and** an `OutboxMessage` atomically in a single Cosmos DB `TransactionalBatch`. A background `OutboxProcessor` polls for pending messages and dispatches them via a registered `IOutboxDispatcher`.

Because the order and its outbox message share the same partition key and are written in a single transactional batch, they succeed or fail atomically — eliminating the dual-write problem entirely.

### How It Works

1. `OrderService` calls `IOutboxWriter.WriteAsync(document, event, partitionKey, destination)` which executes a Cosmos `TransactionalBatch` that atomically creates the business document and the outbox message.
2. `OutboxProcessor` (a `BackgroundService`) polls the outbox on a configurable interval (`OutboxConfig:PollingIntervalSeconds`).
3. Pending messages are dispatched by the matching `IOutboxDispatcher` (e.g. `EventGridOutboxDispatcher`).
4. On success, the message status is updated to `Processed`. On failure, the retry count is incremented and the error is recorded.

### Configuration

```json
{
  "OutboxConfig": {
    "PollingIntervalSeconds": 5,
    "MaxRetryCount": 3,
    "BatchSize": 25,
    "IsEnabled": true
  }
}
```

### Adding a New Outbox Destination

1. Add a constant to `OutboxDestinations`.
2. Create a new class implementing `IOutboxDispatcher` with `Destination` matching the new constant.
3. Register the dispatcher in `Program.cs` as a keyed/enumerable `IOutboxDispatcher`.
4. When publishing, pass the new destination to `IOutboxWriter.WriteAsync(document, event, partitionKey, OutboxDestinations.YourDestination)`.

### Testing

In component tests, `InMemoryEventGridOutboxDispatcher` replaces `EventGridOutboxDispatcher` so dispatched events flow into the same `InMemoryEventGridPublisherStore` used by test assertions. `InMemoryTransactionalBatch` provides an in-memory implementation of Cosmos `TransactionalBatch` so the atomic write path is exercised in tests. The test `OutboxConfig` uses a 1-second polling interval for fast feedback. Outbox-specific steps are in `OutboxSteps` and are skipped in post-deployment mode since the outbox store is unavailable.

## Kafka Events [↑](#top)<a name="kafka-events"></a>

### Publishing Events

The project uses Kafka for publishing recipe log events to the `breakfast_recipe_logs` topic. Events are published via `KafkaEventPublisher<RecipeLogEvent>`.

To publish a new Kafka event:

1. Create a new event class following the pattern of `RecipeLogEvent`, placing it in the appropriate feature folder under `Events/`.
2. Add the new entry into the `ProducerConfigurations` dictionary in the `KafkaConfig` appsettings section using the key as the same name as the event class, with properties `TopicName`, `ApiKey`, `ApiSecret`. Ensure the key exactly matches the event class name.
3. Add the new topic to the `docker-compose-messaging.yml` file, on the line starting `/opt/bitnami/kafka/bin/kafka-topics.sh --create`.
4. Add `ConsumerConfigurations` in `appsettings.componenttests.json` using the same values for `TopicName`, `ApiKey`, `ApiSecret`.
5. Add a Kafka consumer to `StartKafkaConsumers()` in `ConfiguredLightBddScopeAttribute.cs`.
6. Inject `KafkaEventPublisher<YourEvent>` into the constructor of the class that publishes the event, instantiate the event, and call `.PublishEvent()`.
7. Add a Kafka test assertion to the relevant test scenario.

### Consuming Events

To consume a new Kafka event:

1. Add the new entry into the `ConsumerConfigurations` dictionary in the `KafkaConfig` appsettings section using the key as the same name as the event class, with properties `TopicName`, `ApiKey`, `ApiSecret`. Ensure the key exactly matches the event class name.
2. Add the new topic to the `docker-compose-messaging.yml` file, on the line starting `/opt/bitnami/kafka/bin/kafka-topics.sh --create`.
3. Add `ProducerConfigurations` in `appsettings.componenttests.json` using the same values for `TopicName`, `ApiKey`, `ApiSecret`.

### Kafka Variables Configuration

When a new topic is created for Kafka, each topic has its own API key and secret stored in a shared key vault. For deployment, add the following variables:

1. `app_service_app_settings.KafkaConfig__ProducerConfigurations__{EVENT_NAME}__ApiKey`
2. `app_service_app_settings.KafkaConfig__ProducerConfigurations__{EVENT_NAME}__ApiSecret`
3. `app_service_app_settings.KafkaConfig__ProducerConfigurations__{EVENT_NAME}__TopicName`

## Resources [↑](#top)<a name="resources"></a>

### Project Links

- [GitHub Repository](https://github.com/<your-org>/Platform.Templates.ComponentTests.BreakfastProvider)

### Documentation

- [OpenAPI Spec](docs/openapi.json)
- [AsyncAPI Spec](docs/asyncapi.json)
- [Component Specifications](docs/ComponentSpecifications.yml)
