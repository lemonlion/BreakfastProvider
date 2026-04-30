# Test Infrastructure Reference

Test modes, fake services, in-process ASP.NET Core fakes, report generation, and CI pipeline.

## Test Models

Component tests **must not** reference request/response models from `src/`. Tests define their own copies under `tests/BreakfastProvider.Tests.Component/Models/`:

- HTTP request/response models: `tests/BreakfastProvider.Tests.Component/Models/` (e.g. `Models/Pancakes/`, `Models/Orders/`)
- Only use `[JsonPropertyName]` when the serialized name **differs** from default `System.Text.Json` camelCase

### Model bridging

Production code uses src-typed models. If needed, `MappingPublishedEventStore<TSrc, TDest>` can adapt between source and test model types via JSON round-trip serialisation.

Step classes and assertion classes **must only import** test model namespaces — never src model namespaces.

## Component Test Run Modes: InMemory vs Docker vs External SUT vs Post-Deployment

Tests run in **four modes**, controlled by `ComponentTestSettings` flags:

| Mode | Description | API runs via |
|---|---|---|
| **InMemory** (default) | All dependencies replaced with in-memory fakes. No Docker needed. Fastest. | In-process `WebApplicationFactory` |
| **Docker** | Dependencies run as Docker containers. API still runs in-process. Validates real I/O paths. | In-process `WebApplicationFactory` |
| **External SUT** | API and all dependencies run in Docker containers. Validates Docker packaging, config, and startup. Uses post-deployment mode settings. | Docker container (port 5080) |
| **Post-Deployment** | Tests target a fully deployed external API. No in-process services, no fakes, no direct infrastructure access. Only HTTP endpoint assertions run; infrastructure-dependent steps are skipped. | External HTTP endpoint |

### Mode comparison

| Capability | InMemory | Docker | External SUT | Post-Deployment |
|---|---|---|---|---|
| Cow / Goat / Supplier / Kitchen Services | In-process `WebApplicationFactory` fakes | Docker containers | Docker containers | Real downstream services |
| EventGrid | `InMemoryFakeEventGridPublisher` | EventGrid simulator → Azurite storage queue | EventGrid simulator → Azurite storage queue | Production EventGrid (no access) |
| Kafka | `TrackedKafkaProducer` (in-memory) | Docker Kafka broker (`:9092`) | Docker Kafka broker (`:9092`) | Production Kafka (no access) |
| Database | In-memory fake | Cosmos DB emulator | Cosmos DB emulator | Production database (no direct access) |
| API | In-process `WebApplicationFactory` | In-process `WebApplicationFactory` | Docker container (`:5080`) | Deployed service |
| Downstream request inspection | `FakeRequestStore` | `FakeRequestStore` | Unavailable | Unavailable |
| Config overrides | `delayAppCreation` pattern | `delayAppCreation` pattern | Not possible | Not possible |
| Speed | ~20s full suite | ~70s full suite | ~70s + container startup | Depends on network |

### Settings flags (all default to `true`)

| Flag | InMemory | Docker |
|---|---|---|
| `RunWithAnInMemoryCowService` | `true` | `false` |
| `RunWithAnInMemoryGoatService` | `true` | `false` |
| `RunWithAnInMemorySupplierService` | `true` | `false` |
| `RunWithAnInMemoryKitchenService` | `true` | `false` |

Additional URL settings:

| Setting | Default | Purpose |
|---|---|---|
| `CowServiceBaseUrl` | `http://localhost:5031` | Base URL for Cow Service fake |
| `GoatServiceBaseUrl` | `http://localhost:5032` | Base URL for Goat Service fake |
| `SupplierServiceBaseUrl` | `http://localhost:5033` | Base URL for Supplier Service fake |
| `KitchenServiceBaseUrl` | `http://localhost:5034` | Base URL for Kitchen Service fake |
| `PlantUmlServerBaseUrl` | configurable | PlantUML server for diagram generation |
| `RunAgainstExternalServiceUnderTest` | `false` | Enables post-deployment mode |
| `ExternalServiceUnderTestUrl` | `http://localhost:5080` | Base URL of the deployed API (used when `RunAgainstExternalServiceUnderTest` is `true`) |
| `EnableDockerInSetupAndTearDown` | `false` | When `true`, automatically starts/stops Docker Compose in test setup/teardown via the Docker Compose CLI |
| `SkipDockerTearDown` | `false` | When `true`, Docker containers are left running after test teardown so the next run reuses warm containers (avoids Cosmos emulator cold-start instability) |

### Convenience scripts

`tests/BreakfastProvider.Tests.Component/Configure/` contains scripts to switch `appsettings.componenttests.json` between modes:

| Script (`.bat` / `.sh`) | Mode |
|--------------------------|------|
| `switch-to-inmemory` | All `RunWithAnInMemory*` = true, Docker off, `SkipDockerTearDown` = false |
| `switch-to-docker` | All `RunWithAnInMemory*` = false, Docker on, `SkipDockerTearDown` = true |
| `switch-to-docker-external-sut` | Same as Docker + `RunAgainstExternalServiceUnderTest` = true, `SkipDockerTearDown` = true |
| `switch-to-manual-docker-external-sut` | `RunAgainstExternalServiceUnderTest` = true, `EnableDockerInSetupAndTearDown` = false, `SkipDockerTearDown` = false — for manually started Docker containers |

### How mode swap works in code

- `BaseFixture` static constructor checks each flag, conditionally calls swap methods
- When `true`: in-process fake services are started via `InMemoryFakeHelper.Create<TProgram>(url)` (real Kestrel servers bound to TCP ports)
- When `false`: real HTTP calls go to Docker containers
- When `RunAgainstExternalServiceUnderTest` is `true`: no `WebApplicationFactory` is created; `CreateTestClient()` returns a plain `HttpClient` pointing at `ExternalServiceUnderTestUrl`

### External SUT (Docker) mode

External SUT mode runs the BreakfastProvider API in a Docker container alongside all its Docker dependencies. It uses the same `RunAgainstExternalServiceUnderTest` flag as post-deployment mode, so the same `[SkipStepIf]` / `[IgnoreIf]` decorators apply.

**Purpose:** Catches Docker packaging, environment variable configuration, and startup issues before code reaches Dev & Staging environments.

### Automatic Docker lifecycle (`EnableDockerInSetupAndTearDown`)

When `EnableDockerInSetupAndTearDown` is `true`, `DockerComposeOrchestrator` (in `Infrastructure/DockerComposeOrchestrator.cs`) uses the Docker Compose CLI to automatically start Docker Compose before tests and tear it down afterwards. This eliminates the need to manually run batch scripts.

| `EnableDockerInSetupAndTearDown` | `RunAgainstExternalServiceUnderTest` | Compose files started |
|---|---|---|
| `false` | any | None — Docker must be started manually |
| `true` | `false` | `database`, `storage`, `fakes`, `messaging`, `prometheus`, `grafana`, `jaeger` |
| `true` | `true` | `database`, `storage`, `fakes`, `messaging`, `prometheus`, `grafana`, `jaeger`, `sut` |

The orchestrator:
- Resolves the `docker/` directory by walking up from the test working directory
- Uses `ForceBuild()` to rebuild images from source on each run
- Waits for Cosmos DB port `8081/tcp` (120s timeout) before proceeding
- When `RunAgainstExternalServiceUnderTest` is `true`, also waits for the SUT health endpoint at `http://localhost:5080/health` (120s timeout)
- Disposes (tears down) all containers when tests complete, unless `SkipDockerTearDown` is `true`

**How it runs:**
- `docker-compose-sut.yml` builds and starts the API container (port `5080`) on top of all other compose files (database, storage, fakes, messaging, prometheus, grafana, jaeger)
- The API's downstream service URLs are overridden via environment variables to point at Docker service names (e.g. `CowServiceConfig__BaseAddress=http://cow-service:8080`)
- `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger.local:4317` sends traces to the Jaeger container
- `docker-entrypoint.sh` installs certificates from `/certs/` before starting the app
- Tests set `RunAgainstExternalServiceUnderTest=true` and `ExternalServiceUnderTestUrl=http://localhost:5080`

**Local usage:** Run `/docker/docker-compose-external-sut-up.bat` (or `.sh`)

**Manual Docker usage:** Start containers with `/docker/docker-compose-external-sut-up.bat`, then switch to manual mode with `switch-to-manual-docker-external-sut.bat` (sets `EnableDockerInSetupAndTearDown: false` so the test framework won't tear down your containers).

**CI:** The `component-tests-external-sut` job in `ci-pull-request.yml` calls `_tests.yml` with `test-run-type: external-sut`

### External Service Under Test mode

When `RunAgainstExternalServiceUnderTest` is `true`, the tests target a fully deployed external API rather than the in-process `WebApplicationFactory`. In this mode, the following infrastructure is **unavailable**:

- **Event store** — no in-memory `IPublishedEventStore` to inspect published events
- **Kafka message store** — no `IKafkaMessageStore` to inspect published Kafka messages
- **Downstream fake request store** — no `FakeRequestStore` to inspect outbound HTTP requests
- **Database** — no direct Cosmos DB access for setup/teardown
- **Config switching** — no ability to override configuration; tests can only exercise the deployed service's default config

#### Two skip mechanisms

| Mechanism | Scope | When to use |
|---|---|---|
| `[IgnoreIf]` | Whole scenario | The scenario's **setup or primary assertion** depends on controlling fakes or accessing infrastructure that doesn't exist in post-deployment mode |
| `[SkipStepIf]` | Single step | The step's assertion depends on infrastructure unavailable in post-deployment mode, but the rest of the scenario can still run |

#### `[IgnoreIf]` — scenario-level skip

Use when the entire scenario cannot run in post-deployment mode (e.g. it controls fake responses, inspects fake request content, or its primary assertions require event/kafka infrastructure). The attribute accepts one or more reasons via `params` — they are joined with `"; "` in the skip message:

```csharp
[Scenario]
[IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), IgnoreReasons.NeedsEventAndKafkaInfrastructure)]
public async Task A_Valid_Order_Should_Be_Created_And_An_Event_Published()
```

Scenario-level `IgnoreReasons` constants:

| Constant | Use when… |
|---|---|
| `NeedsToControlFakeResponses` | Given steps configure fake error responses (e.g. `X-Fake-CowService-Scenario: ServiceUnavailable`) |
| `NeedsDirectDatabaseAccess` | Scenario reads/writes database directly |
| `NeedsNonDefaultConfiguration` | Scenario uses `delayAppCreation: true` with config overrides |
| `NeedsEventAndKafkaInfrastructure` | Scenario's primary purpose is verifying event/Kafka publication |

#### `[SkipStepIf]` — step-level skip

Use on step methods whose assertions depend on infrastructure unavailable in post-deployment mode, within scenarios that **otherwise can run**. The attribute is an `IStepDecoratorAttribute` that calls `StepExecution.Current.Bypass(reason)` — the step appears as **bypassed** (not passed) in LightBDD reports.

Works with both `Task` and `Task<CompositeStep>` return types:

```csharp
[SkipStepIf(nameof(ComponentTestSettings.RunAgainstExternalServiceUnderTest), IgnoreReasons.DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
private async Task The_cow_service_should_have_received_a_milk_request()
    => _downstreamSteps.AssertCowServiceReceivedMilkRequest();

[SkipStepIf(nameof(ComponentTestSettings.RunAgainstExternalServiceUnderTest), IgnoreReasons.EventStoreIsUnavailableInPostDeploymentEnvironments)]
private async Task An_order_created_event_should_have_been_published()
{ /* event store assertions */ }
```

Step-level `IgnoreReasons` constants:

| Constant | Used in |
|---|---|
| `EventStoreIsUnavailableInPostDeploymentEnvironments` | Event assertion steps |
| `DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments` | Downstream service assertion steps |
| `KafkaIsUnavailableInPostDeploymentEnvironments` | Kafka message assertion steps |
| `OutboxStoreIsUnavailableInPostDeploymentEnvironments` | Outbox message assertion steps |

#### `SkipStepIfAttribute` classes

Two classes implement the step-level skip:

- **Generic:** `Tests.Component.LightBddCustomisations.SkipStepIfAttribute<T>` — implements `IStepDecoratorAttribute`, reads the named boolean property from the fixture's `IIgnorable<T>.IgnoreSettings`, and calls `StepExecution.Current.Bypass(reason)` when `true`.
- **Non-generic:** `Tests.Component.Infrastructure.SkipStepIfAttribute` — extends `SkipStepIfAttribute<ComponentTestSettings>` for convenience (no type parameter needed).

#### `IgnoreReasons` constants file

All skip reason strings live in `tests/BreakfastProvider.Tests.Component/Constants/IgnoreReasons.cs`. When adding a new reason, add it there — never use inline string literals.

#### Step classes with `[SkipStepIf]` guards

| Feature steps file | Step method | Infrastructure unavailable |
|---|---|---|
| `Pancakes__Creation_Feature.steps.cs` | `The_cow_service_should_have_received_a_milk_request()` | Fake request store |
| `Pancakes__Creation_Feature.cs` | `A_Pancake_Request_With_More_Toppings_Than_Allowed_Should_Return_A_Bad_Request_Response` | Config access (`NeedsNonDefaultConfiguration`) |
| `Waffles__Creation_Feature.steps.cs` | `The_cow_service_should_have_received_a_milk_request()` | Fake request store |
| `Waffles__Creation_Feature.cs` | `A_Waffle_Request_With_More_Toppings_Than_Allowed_Should_Return_A_Bad_Request_Response` | Config access (`NeedsNonDefaultConfiguration`) |
| `AuditLogs__Retrieval_Feature.steps.cs` | `The_cow_service_should_have_received_a_milk_request()` | Fake request store |
| `AuditLogs__Retrieval_Feature.steps.cs` | `The_kitchen_service_should_have_received_a_preparation_request()` | Fake request store |
| `Orders__Order_Retrieval_Feature.steps.cs` | `The_cow_service_should_have_received_a_milk_request()` | Fake request store |
| `Orders__Order_Retrieval_Feature.steps.cs` | `The_kitchen_service_should_have_received_a_preparation_request()` | Fake request store |
| `Orders__Breakfast_Order_Feature.steps.cs` | `An_order_created_event_should_have_been_published()` | Event store |
| `Orders__Breakfast_Order_Feature.steps.cs` | `The_kitchen_service_should_have_received_a_preparation_request()` | Fake request store |
| `Orders__Breakfast_Order_Feature.steps.cs` | `A_recipe_log_should_have_been_published_to_kafka()` | Kafka message store |
| `Orders__Breakfast_Order_Feature.steps.cs` | `An_outbox_message_should_have_been_written_for_the_order_created_event()` | Outbox store |
| `Orders__Breakfast_Order_Feature.steps.cs` | `The_outbox_message_should_have_been_processed()` | Outbox store |
| `Menu__Availability_Feature.steps.cs` | `The_supplier_service_should_have_received_an_availability_request()` | Fake request store |
| `Ingredients__Goat_Milk_Sourcing_Feature.steps.cs` | `The_goat_service_should_have_received_a_goat_milk_request()` | Fake request store |
| `DailySpecials__Ordering_Feature.cs` | `Ordering_A_Daily_Special_Beyond_The_Threshold` | Config access (`NeedsNonDefaultConfiguration`) |
| `DailySpecials__Ordering_Feature.cs` | `Remaining_Quantity_Should_Decrease_After_Each_Order` | Config access (`NeedsNonDefaultConfiguration`) |

## Delayed App Creation

Features using `base(delayAppCreation: true)` delay `WebApplicationFactory` creation so GIVEN steps can accumulate config overrides before the app starts. Step classes **must not** be resolved until after `CreateAppAndClient(overrides)` is called.

> **Post-deployment mode:** `delayAppCreation` features are incompatible with post-deployment mode because the deployed service has fixed configuration. All scenarios in these features must be annotated with `[IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), IgnoreReasons.NeedsNonDefaultConfiguration)]`.

See [composite-patterns.md](composite-patterns.md#config-as-steps-with-lazy-app-creation) for the full `EnsureAppCreated()` pattern.

## Global Setup / Teardown

`ConfiguredLightBddScopeAttribute` handles test lifecycle:

1. **Setup** — Start in-memory fake services via `InMemoryFakeHelper.Create<TProgram>(url)`, configure LightBDD report generation, start event/diagram tracking
2. **Per-test** — Each scenario gets unique IDs and a fresh HTTP client
3. **Teardown** — Stop fake services, copy specification files to `docs/`, validate YAML contains no instance data

### Startup order

```
1. Start HTTP fakes (in-process Kestrel servers)
2. Initialize host (calls BaseFixture.EnsureHostInitialized)
3. Configure report writers
```

## Fake Services

Fake services are **standalone ASP.NET Core Minimal API projects** under `fakes/` that mimic external dependencies. They are NOT mock/stub frameworks — they are fully functional HTTP APIs with their own `Program.cs` and inline endpoint definitions.

### Fake Project Structure

```
fakes/
├── Dependencies.Fakes.CowService/
│   ├── Program.cs                    # GET /milk → { "milk": "Some_Fresh_Milk" }
│   └── Dependencies.Fakes.CowService.csproj
├── Dependencies.Fakes.GoatService/
│   ├── Program.cs                    # GET /goat-milk → { "goatMilk": "Some_Fresh_Goat_Milk" }
│   └── Dependencies.Fakes.GoatService.csproj
├── Dependencies.Fakes.SupplierService/
│   ├── Program.cs                    # GET /ingredients/{name}/availability
│   └── Dependencies.Fakes.SupplierService.csproj
└── Dependencies.Fakes.KitchenService/
    ├── Program.cs                    # POST /prepare, GET /status/{orderId}
    └── Dependencies.Fakes.KitchenService.csproj
```

Each fake uses ASP.NET Core Minimal APIs:
- Endpoints defined inline via `app.MapGet(...)` / `app.MapPost(...)` in `Program.cs`
- No controllers, no `AddControllers()`, no `MapControllers()`
- No HTTPS redirection (avoids redirect loops in the InMemory test harness where fakes are called over HTTP)

### Starting Fakes In-Process — InMemoryFakeHelper

During InMemory tests, fakes are spun up **in-process** using `WebApplicationFactory<TProgram>` via `InMemoryFakeHelper.Create<TProgram>(url)`. This creates a real Kestrel server bound to the configured TCP port — tests make real HTTP calls to fakes running on URLs like `http://localhost:5031`.

```csharp
public static WebApplicationFactoryForSpecificUrl<TProgram> Create<TProgram>(string baseUrl, IConfiguration? config = null)
    where TProgram : class
{
    HttpFakesHelper.AssertPortIsNotInUse(baseUrl);
    var fixture = new WebApplicationFactoryForSpecificUrl<TProgram>(hostUrl: baseUrl, config);
    fixture.CreateDefaultClient();
    return fixture;
}
```

#### WebApplicationFactoryForSpecificUrl

The key innovation: returns a dummy host but starts a **real Kestrel server**:

```csharp
protected override IHost CreateHost(IHostBuilder builder)
{
    var dummyHost = builder.Build();
    _realHost = builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel()).Build();
    _realHost.Start();  // Actual TCP server listening on the configured port
    return dummyHost;   // Return dummy so CreateDefaultClient() works
}
```

### Starting Fakes in ConfiguredLightBddScopeAttribute

```csharp
private void StartHttpFakes()
{
    if (Settings.RunWithAnInMemoryCowService)
        _cowServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.CowService.Program>(
            Settings.CowServiceBaseUrl);

    if (Settings.RunWithAnInMemoryGoatService)
        _goatServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.GoatService.Program>(
            Settings.GoatServiceBaseUrl);

    if (Settings.RunWithAnInMemorySupplierService)
        _supplierServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.SupplierService.Program>(
            Settings.SupplierServiceBaseUrl);

    if (Settings.RunWithAnInMemoryKitchenService)
        _kitchenServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.KitchenService.Program>(
            Settings.KitchenServiceBaseUrl);
}
```

### Controlling Fake Responses Per-Request

Fake services use **header-based scenario selection**. Tests set headers like `X-Fake-Scenario` on the inbound request; the API forwards them to the fake via `FakeHeaderPropagationHandler`, which reads the header and returns the appropriate canned response. This works identically in both InMemory and Docker modes.

Step classes that make HTTP calls (e.g. `GetMilkSteps`, `GetGoatMilkSteps`, `GetMenuSteps`, `PostOrderSteps`) expose an `AddHeader(name, value)` method to set extra headers on their outbound requests. Use this from scenario GIVEN steps to configure fake behaviour:

```csharp
// In a step class — AddHeader accumulates extra headers
public void AddHeader(string name, string value) => _extraHeaders[name] = value;

// In Retrieve()/Send() — merge extra headers into the request
foreach (var header in _extraHeaders)
    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
```

```csharp
// In a test step — set a header to control fake behaviour
private async Task The_cow_service_will_return_service_unavailable()
{
    _getMilkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.ServiceUnavailable);
}
```

Fake scenario constants live in `tests/.../Constants/FakeScenarios.cs`:
- `FakeScenarios` — scenario values: `ServiceUnavailable`, `Timeout`, `OutOfStock`, `KitchenBusy`
- `FakeScenarioHeaders` — header names: `CowService`, `GoatService`, `SupplierService`, `KitchenService`
- `DownstreamErrorMessages` — expected error message strings for assertions

```csharp
// In the fake's Program.cs — read the header and return the scenario response
app.MapGet("/milk", (HttpContext context) =>
{
    var scenario = context.Request.Headers["X-Fake-CowService-Scenario"].FirstOrDefault();

    return scenario switch
    {
        "ServiceUnavailable" => Results.StatusCode(503),
        "Timeout" => Results.StatusCode(504),
        _ => Results.Ok(new { Milk = "Some_Fresh_Milk" })
    };
});
```

### Cache Clearing for Parallel Safety

When testing caching behaviour (e.g. `Menu__Caching_Feature`) or scenarios where cached results from parallel tests could interfere (e.g. `Menu__Downstream_Failure_Feature`), clear the cache in a GIVEN step before exercising the endpoint:

```csharp
private async Task The_menu_cache_is_cleared()
{
    var response = await Client.DeleteAsync($"/{Endpoints.Menu}/cache");
    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

This ensures deterministic results even when tests run in parallel.

### Port Availability Check

Before starting a fake, verify the port is available:

```csharp
public static class HttpFakesHelper
{
    public static void AssertPortIsNotInUse(string url)
    {
        var uri = new Uri(url);
        using var listener = new TcpListener(IPAddress.Loopback, uri.Port);
        try { listener.Start(); listener.Stop(); }
        catch { throw new InvalidOperationException($"Port {uri.Port} is already in use"); }
    }
}
```

## HTTP Client Handler Chain

When the API under test makes outgoing HTTP calls, they pass through a handler chain that provides tracking and capture:

```
FakeHeaderPropagationHandler
  ↓ (propagates X-Fake-* headers from inbound test request to outbound calls)
RequestCapturingHandler
  ↓ (records request to FakeRequestStore, keyed by X-ComponentTest-RequestId)
TestTrackingMessageHandler
  ↓ (logs request/response to PlantUML diagram)
HttpClientHandler
  ↓
[Fake service or real Docker service]
```

### FakeHeaderPropagationHandler

A `DelegatingHandler` registered on named `HttpClient` instances. It reads `X-Fake-*` headers from the current `HttpContext` (via `IHttpContextAccessor`) at `SendAsync` time and forwards them to outbound calls. This allows tests to control fake behaviour per-request without shared mutable state.

### RequestCapturingHandler

A `DelegatingHandler` that captures every outgoing request keyed by the test's `X-ComponentTest-RequestId` header:

```csharp
var requestId = httpContextAccessor.HttpContext?.Request.Headers[CustomHeaders.ComponentTestRequestId]
    .FirstOrDefault();
if (requestId != null)
{
    store.Add(requestId, new CapturedHttpRequest(clientName, request.Method, request.RequestUri, headers, body));
}
```

### FakeRequestStore

Thread-safe, per-request-ID storage for captured outbound requests:

```csharp
public class FakeRequestStore
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<CapturedHttpRequest>> _requests = new();

    public void Add(string requestId, CapturedHttpRequest request)
    {
        _requests.GetOrAdd(requestId, _ => new ConcurrentBag<CapturedHttpRequest>()).Add(request);
    }

    public IReadOnlyList<CapturedHttpRequest> GetRequests(string requestId, string clientName)
    {
        return GetRequests(requestId)
            .Where(r => r.ClientName.Equals(clientName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
```

## BaseFixture

`BaseFixture` is the abstract base class for all feature classes. It extends `FeatureFixture` and manages:

1. **Static `WebApplicationFactory<Program>`** — created once, shared across all features using default config
2. **`TestTrackingMessageHandler`** — wraps `HttpClient` to log all HTTP interactions for PlantUML diagrams
3. **`TestTrackingHttpClientFactory`** — replaces `IHttpClientFactory`; chains `FakeHeaderPropagationHandler → RequestCapturingHandler → TestTrackingMessageHandler → HttpClientHandler`
4. **`Client`** property — the HTTP client for calling the API under test
5. **`Get<T>()`** — resolves step classes and services from the per-fixture test `ServiceProvider`
6. **`AppFactory`** — exposes the `WebApplicationFactory` for accessing `Services` (DI container)
7. **`CreateAppAndClient(overrides, additionalServices)`** — for `delayAppCreation: true` features, creates a new app with config overrides and/or additional service registrations
8. **`CreateAppAndClientWithSharedFactory(overrides)`** — Docker mode: reuses a shared factory for identical overrides to avoid redundant host startups
9. **`FakeRequestStore`** — shared store capturing outgoing HTTP requests for downstream assertions
10. **`RequestId`** — per-fixture `Guid.NewGuid().ToString()`, used for `X-ComponentTest-RequestId` correlation
11. **`RequestContext`** — singleton injected into step classes carrying `Func<HttpClient>` and `RequestId`

### Step Class DI & `Get<T>()`

Step classes are resolved via `Get<T>()` from a per-fixture `ServiceCollection` built in the `BaseFixture` constructor:

```csharp
var services = new ServiceCollection();
services.AddSingleton(new RequestContext(() => Client, RequestId));

// Step classes — transient so each Get<T>() returns a fresh instance
services.AddTransient<GetMilkSteps>();
services.AddTransient<GetEggsSteps>();
services.AddTransient<GetFlourSteps>();
services.AddTransient<GetGoatMilkSteps>();
services.AddTransient<PostPancakesSteps>();
services.AddTransient<PostWafflesSteps>();
services.AddTransient<PostOrderSteps>();
services.AddTransient<GetOrderSteps>();
services.AddTransient<PostToppingsSteps>();
services.AddTransient<GetToppingsSteps>();
services.AddTransient<GetMenuSteps>();
services.AddTransient<GetAuditLogsSteps>();
services.AddTransient<DownstreamRequestSteps>();
services.AddTransient<OutboxSteps>();
services.AddTransient<PatchOrderStatusSteps>();
services.AddTransient<DeleteToppingSteps>();
```

`RequestContext` is registered as a **singleton** carrying a `Func<HttpClient>` (deferred via `() => Client`) and the `RequestId`. The `Func<>` ensures step classes receive the correct `HttpClient` even when `CreateAppAndClient()` is called after construction (as in `delayAppCreation: true` features).

### RequestContext

```csharp
public class RequestContext(Func<HttpClient> clientFactory, string requestId)
{
    public HttpClient Client => clientFactory();
    public string RequestId { get; } = requestId;
}
```

Step classes accept `RequestContext` via constructor injection:

```csharp
public class GetMilkSteps(RequestContext context)
{
    public async Task Retrieve() => ... context.Client ... context.RequestId ...
}

public class DownstreamRequestSteps(FakeRequestStore fakeRequestStore, RequestContext context)
{
    // Uses both FakeRequestStore and RequestContext
}
```

Feature `.steps.cs` files resolve step classes in their constructor:

```csharp
public partial class Pancakes__Creation_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Pancakes__Creation_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private async Task Milk_is_retrieved()
        => await _milkSteps.Retrieve();
}
```

### TestTrackingHttpClientFactory

Replaces the production `IHttpClientFactory`. Chains the handler pipeline and labels downstream service calls in PlantUML diagrams:

```csharp
public HttpClient CreateClient(string name)
{
    var label = name switch
    {
        "CowService" => "Cow Service",
        "GoatService" => "Goat Service",
        "SupplierService" => "Supplier Service",
        "KitchenService" => "Kitchen Service",
        _ => "Breakfast Provider"
    };

    var handler = new FakeHeaderPropagationHandler(_httpContextAccessor,
        new RequestCapturingHandler(_fakeRequestStore, _httpContextAccessor, name,
            new TestTrackingMessageHandler(label, new HttpClientHandler())));

    return new HttpClient(handler) { BaseAddress = new Uri(_serviceUrls[name]) };
}
```

### DI Registration in ConfigureTestServices

```csharp
// Remove production implementations
services.RemoveAll<IHttpClientFactory>();

// Register test handler chain
services.AddSingleton<IHttpClientFactory>(sp =>
    new TestTrackingHttpClientFactory(
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<FakeRequestStore>(),
        serviceUrls));

// Register shared stores
services.AddSingleton<FakeRequestStore>();
```

## Outbox Test Infrastructure

The transactional outbox pattern writes the business document (e.g. `OrderDocument`) and its `OutboxMessage` atomically in a single Cosmos `TransactionalBatch`. Both items share the same partition key, guaranteeing they succeed or fail together. A background `OutboxProcessor` polls for pending messages and dispatches them via `IOutboxDispatcher`.

### In-Memory Transactional Batch

`InMemoryTransactionalBatch` (in `Fakes/Cosmos/InMemoryTransactionalBatch.cs`) implements Cosmos `TransactionalBatch` for in-memory tests. It collects `CreateItem` operations and applies them atomically to the `InMemoryContainer`'s backing store on `ExecuteAsync`. If any item already exists, the entire batch fails with `Conflict` status — mirroring real Cosmos transactional batch behaviour.

`InMemoryCosmosContainer.CreateTransactionalBatch()` returns an `InMemoryTransactionalBatch` sharing the same `ConcurrentDictionary` backing store.

### In-Memory Outbox Dispatcher

In InMemory test mode, `InMemoryEventGridOutboxDispatcher` replaces `EventGridOutboxDispatcher`. It writes raw JSON payloads directly to `InMemoryEventGridPublisherStore`, so dispatched events end up in the same store as directly-published events and can be verified with the same assertions.

Registration happens in `UseInMemoryEventGrid()`:

```csharp
services.RemoveAll<IOutboxDispatcher>();
services.AddSingleton<IOutboxDispatcher>(sp =>
    new InMemoryEventGridOutboxDispatcher(sp.GetRequiredService<InMemoryEventGridPublisherStore>()));
```

### OutboxSteps

`OutboxSteps` is a reusable step class (in `Common/Orders/OutboxSteps.cs`) injected via `BaseFixture`'s DI:

```csharp
public class OutboxSteps(ICosmosRepository<OutboxMessage> outboxRepository)
{
    public IReadOnlyList<TestOutboxMessage>? OutboxMessages { get; private set; }

    public async Task LoadOutboxMessages() { /* queries Cosmos */ }
    public void AssertOutboxContainsMessageForEventType(string eventType) { /* ... */ }
    public void AssertOutboxMessageWasProcessed(string eventType) { /* ... */ }
}
```

### Outbox Bridge in BaseFixture

`BaseFixture` bridges `ICosmosRepository<OutboxMessage>` from the app's DI container so `OutboxSteps` can read outbox documents:

```csharp
services.AddSingleton(sp => AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>());
```

### Outbox Assertion Pattern with Polling

Because the `OutboxProcessor` runs on a background timer, outbox assertions use a retry-polling pattern (same as event/Kafka assertions):

```csharp
const int maxRetries = 50;
var retryDelay = TimeSpan.FromMilliseconds(200);

for (var i = 0; i < maxRetries; i++)
{
    await _outboxSteps.LoadOutboxMessages();
    if (_outboxSteps.OutboxMessages!.Any(m => m.EventType == "OrderCreatedEvent"))
        break;
    await Task.Delay(retryDelay);
}

_outboxSteps.AssertOutboxContainsMessageForEventType("OrderCreatedEvent");
```

The test `OutboxConfig` uses `PollingIntervalSeconds: 1` for fast feedback.

### Post-Deployment Mode

Outbox steps are decorated with `[SkipStepIf]` using `IgnoreReasons.OutboxStoreIsUnavailableInPostDeploymentEnvironments` since the outbox store (Cosmos DB) is not directly accessible in post-deployment mode.

#### Lazy OutboxSteps Resolution

`OutboxSteps` depends on `ICosmosRepository<OutboxMessage>`, which is bridged from `AppFactory.Services`. In post-deployment mode, `AppFactory` is unavailable, so eagerly resolving `OutboxSteps` in I constructor would throw. Features that use `OutboxSteps` with `[SkipStepIf]` guards must resolve it **lazily**:

```csharp
// WRONG — crashes in post-deployment mode even though steps are [SkipStepIf]-guarded:
private readonly OutboxSteps _outboxSteps;
public MyFeature() { _outboxSteps = Get<OutboxSteps>(); }

// CORRECT — lazy resolution defers until the step actually runs:
private OutboxSteps? _outboxSteps;
private OutboxSteps OutboxSteps => _outboxSteps ??= Get<OutboxSteps>();
```

This pattern ensures the DI resolution only happens when the step executes, which won't occur in post-deployment mode thanks to `[SkipStepIf]`.

### TestOutboxMessage

Test-owned model in `Models/Events/TestOutboxMessage.cs` that mirrors the production `OutboxMessage` fields. Test code never references the production `OutboxMessage` model directly (consistent with the test model ownership rule).

## Health Check Test Infrastructure

The API implements ASP.NET Core health checks with custom `IHealthCheck` implementations and a JSON response writer. Health checks are registered with tags and failure statuses per the MS docs standard.

### Dependency Health Checks

| Check Name | Type | Tags | Failure Status | Implementation |
|---|---|---|---|---|
| CowService | Downstream API | `downstream`, `api` | Degraded | `DownstreamServiceHealthCheck` (probes `/health` on fake) |
| GoatService | Downstream API | `downstream`, `api` | Degraded | `DownstreamServiceHealthCheck` |
| SupplierService | Downstream API | `downstream`, `api` | Degraded | `DownstreamServiceHealthCheck` |
| KitchenService | Downstream API | `downstream`, `api` | Degraded | `DownstreamServiceHealthCheck` |
| CosmosDb | Infrastructure | `infrastructure`, `database` | Unhealthy | `CosmosDbHealthCheck` (or `NoOpHealthCheck` in-memory) |
| Kafka | Infrastructure | `infrastructure`, `messaging` | Unhealthy | `KafkaHealthCheck` (or `NoOpHealthCheck` in-memory) |

### In-Memory Mode Handling

- **CosmosDb**: When `RunWithAnInMemoryDatabase` is `true`, `UseInMemoryDatabase()` removes `CosmosClient` from DI. The production health check factory would report **Unhealthy** ("CosmosDb not configured."). `ReplaceCosmosDbHealthCheckWithNoOp()` is called in `ConfigureTestServices` to replace it with a `NoOpHealthCheck` that reports Healthy.
- **Kafka**: When `RunWithAnInMemoryKafkaBroker` is `true`, `ReplaceKafkaHealthCheckWithNoOp()` is called in `ConfigureTestServices` to replace the real Kafka check with `NoOpHealthCheck`.
- **Downstream services**: All 4 fake services expose `GET /health` endpoints, so downstream health checks pass in both in-memory and Docker modes.

### JSON Response Format

The health check endpoint returns a custom JSON response via `HealthCheckResponseWriter`:

```json
{
  "status": "Healthy",
  "results": {
    "CowService": { "status": "Healthy", "description": "CowService is reachable.", "data": {} },
    "CosmosDb": { "status": "Healthy", "description": "CosmosDb not configured.", "data": {} },
    ...
  }
}
```

### Test Model

`TestHealthCheckResponse` in `Models/Infrastructure/` mirrors the JSON response structure. The test asserts the overall status and verifies each dependency entry exists.

## PlantUML Sequence Diagrams & HTML Specification Report

Component tests generate an HTML specification report with embedded PlantUML sequence diagrams.

### Configuration

`ConfiguredLightBddScopeAttribute` configures three report writers:

1. **`ComponentSpecificationsWithExamples.html`** — formatted specs with PlantUML diagrams (for DevPortal)
2. **`ComponentSpecifications.yml`** — plain YAML spec (source-controlled in `/docs/`)
3. **`FeaturesReport.html`** — full report with test run details (for analysis)

### Redaction Pipeline

`HtmlDocumentHelpers.cs` chains transformations to redact sensitive data from PlantUML diagrams. When a new value appears unredacted, add a new `Regex` field and chain a redaction call.

### Specifications YAML

After tests complete, the YAML specification is copied to `docs/ComponentSpecifications.yml`. This source-controlled YAML is the living specification document.

### Instance Data Validation

The framework validates that the YAML specification does not contain instance-specific data (e.g. GUIDs, timestamps). This ensures the specification is deterministic and can be meaningfully diffed in version control.

### Debugging with PlantUML Diagrams

Open `tests/BreakfastProvider.Tests.Component/bin/Debug/net10.0/Reports/FeaturesReport.html` to see:
- Every HTTP request/response
- All downstream calls to Cow Service, Goat Service, etc.
- Complete visual trace without breakpoints

## CI Pipeline

### Workflow Structure

The CI pipeline runs on pull requests to `main`:

1. **get-version** — Outputs SemVer version number
2. **build** — Publishes API, creates NuGet package
3. **component-tests-in-memory** — Runs component tests with in-memory fakes
4. **component-test-in-memory-report** — Parses TRX results to markdown summary
5. **component-test-in-memory-coverage** — Generates code coverage reports
6. **component-tests-external-sut** — Runs component tests against the API running in Docker (external SUT mode)
7. **component-tests-external-sut-report** — Parses TRX results for external SUT run

The `_tests.yml` reusable workflow accepts a `test-run-type` input with values `memory`, `docker`, or `external-sut`.

- **`memory`**: All dependencies are in-memory; no Docker needed.
- **`docker`**: Dependencies run as Docker containers; the API runs in-process.
- **`external-sut`**: All compose files including `docker-compose-sut.yml` are started with `--wait`; the test runner sets `RunAgainstExternalServiceUnderTest=true` and `ExternalServiceUnderTestUrl=http://localhost:5080`.

### Test Running

```powershell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

### Filtering to Specific Features

Use `--filter` with `FullyQualifiedName~`:

```powershell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj --filter "FullyQualifiedName~Pancakes__Creation_Feature"
```

### Generated Artifacts

| Artifact | Source Controlled | Contains Instance Data | Purpose |
|---|---|---|---|
| `ComponentSpecificationsWithExamples.html` | No | Yes (PlantUML diagrams) | DevPortal documentation |
| `ComponentSpecifications.yml` | Yes (`/docs/`) | No | Living specification, git diffs |
| `FeaturesReport.html` | No | Yes | Local/CI analysis |
| `*.trx` | No | Yes | Test results for CI reporting |
| Coverage XML/HTML | No | Yes | Code coverage analysis |

## Additional Service Registration (additionalServices callback)

`CreateAppAndClient` accepts an optional `Action<IServiceCollection>? additionalServices` callback, invoked *after* `ConfigureTestServices`. Use this to inject test-specific service replacements beyond config overrides. Examples:

### Replacing Health Checks with Degraded Status

```csharp
CreateAppAndClient(additionalServices: services =>
{
    services.ReplaceHealthCheckWithDegraded(HealthCheckNames.CowService, "Cow Service unreachable.");
});
```

The `ReplaceHealthCheckWithDegraded()` extension (in `ServiceCollectionExtensions`) removes the named health check and registers a `NoOpHealthCheck` returning `HealthCheckResult.Degraded`.

### Injecting an In-Memory Log Capture

```csharp
private readonly InMemoryLoggerProvider _logProvider = new();

CreateAppAndClient(additionalServices: services =>
{
    services.AddSingleton<ILoggerProvider>(_logProvider);
});

// Later, assert on captured log entries:
_logProvider.Entries.Should().Contain(e => e.Message.Contains("Order created"));
```

`InMemoryLoggerProvider` (in `Common/Logging/`) is a thread-safe `ILoggerProvider` that captures all log messages in a `ConcurrentBag<LogEntry>`. Use it to verify structured logging output in component tests.

### Per-Scenario Database Isolation

By default, features using the standard `BaseFixture` constructor share a single static `WebApplicationFactory` and its `InMemoryContainer`. This means all parallel tests see the same in-memory database. For features that need a clean database (e.g. asserting on empty results), use `delayAppCreation: true` with a no-op `additionalServices` callback to force a per-scenario factory:

```csharp
public Orders__Pagination_Feature() : base(delayAppCreation: true)
{
    // Per-scenario factory ensures an isolated database in in-memory mode.
    CreateAppAndClient(additionalServices: _ => { });

    _milkSteps = Get<GetMilkSteps>();
    // ...
}
```

The `additionalServices` callback (even if empty) triggers `CreateAppAndClient` to create a new `WebApplicationFactory` with its own DI container and `InMemoryContainer`, rather than falling back to the shared static factory.
