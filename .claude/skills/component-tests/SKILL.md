---
name: component-tests
description: Component test conventions for the Breakfast Provider platform. Use when writing, refactoring, or reviewing LightBDD BDD-style component tests in tests/BreakfastProvider.Tests.Component. Covers core rules, parallel-safety, and TDD workflow. See README.md for the full file index.
user-invocable: false
---

# Component Test Conventions — Core Rules

> For detailed patterns, see the [index](README.md).

## Frameworks

| Package | Purpose |
|---|---|
| **xUnit** | Test runner |
| **AwesomeAssertions** | Assertion library (FluentAssertions fork) |
| **NSubstitute** | Mocking (not Moq) |
| **LightBDD.XUnit2** | BDD-style component/acceptance tests |
| **LightBDD.Contrib.ReportingEnhancements** | HTML & YAML report formatting |
| **AutoFixture** | Test data generation |
| **Microsoft.AspNetCore.Mvc.Testing** | `WebApplicationFactory` for API component tests |
| **In-process ASP.NET Core fakes** | Standalone Minimal API projects under `fakes/` started via `InMemoryFakeHelper` |
| **TestTrackingDiagrams.LightBDD.XUnit** | Test tracking & PlantUML dependency diagrams |

### Global Usings

```csharp
global using AwesomeAssertions;
global using Xunit;
```

## Project Structure

```
tests/BreakfastProvider.Tests.Component/
├── Common/           # Reusable step classes shared across features
│   ├── Pancakes/     # Pancake-specific steps (ingredients, toppings, downstream)
│   ├── Waffles/      # Waffle-specific steps
│   ├── Orders/       # Order-specific steps
│   └── ...
├── Constants/        # Shared constants (endpoints, validation messages, test data)
├── Fakes/             # Test wiring for in-process ASP.NET Core fakes (helper extensions, handler chain)
│   └── ...
├── Infrastructure/    # BaseFixture, ConfiguredLightBddScopeAttribute, settings
├── LightBddCustomisations/  # HTML report customisation, redaction
├── Models/            # Test-owned request/response/event models (never reference src/ models)
├── Scenarios/
│   ├── Pancakes/      # Pancake feature files (.cs) and step files (.steps.cs)
│   ├── Waffles/       # Waffle feature files
│   ├── Orders/        # Order feature files
│   ├── Toppings/      # Topping feature files
│   ├── Menu/          # Menu feature files
│   └── Ingredients/   # Ingredient sourcing feature files
├── Stylesheets/       # Custom CSS for generated reports
├── Util/              # Sub, StartWith, JSON helpers, test utilities
└── appsettings.componenttests.json
```

Each feature has two files:
- **`{Feature}_Feature.cs`** — scenario definitions (GIVEN/WHEN/THEN wiring) as a partial class
- **`{Feature}_Feature.steps.cs`** — step implementations in the other partial class

## Hard Rules

### NEVER
- **NEVER** use `[Collection("...")]` or `[CollectionDefinition]` — these serialize tests and destroy parallelism
- **NEVER** share mutable state across feature classes — each feature class is its own parallel unit
- **NEVER** use `Assert.*` or FluentAssertions — use AwesomeAssertions `.Should()` exclusively
- **NEVER** use Moq — use NSubstitute
- **NEVER** skip the downstream service assertions for success-path scenarios that call external services
- **NEVER** use numeric status codes in scenario names — use named outcomes (see [naming-conventions.md](naming-conventions.md))
- **NEVER** pass dynamic values (`Guid.NewGuid()`, `Random.Shared.NextInt64()`, `DateTime.UtcNow`) as step parameters — they break stable YAML specifications
- **NEVER** reference request/response models from `src/` in tests — use test-owned copies under `tests/BreakfastProvider.Tests.Component/Models/`
- **NEVER** use arbitrary round numbers for test quantities — use config-derived expressions (see [composite-patterns.md](composite-patterns.md))
- **NEVER** use magic strings when a constant, `nameof()`, or framework type exists — see [Constants Over Magic Strings](#constants-over-magic-strings) below
- **NEVER** use constants or `nameof()` inside `[Inputs]`, `[Outputs]`, `[HeadIn]`, or `[HeadOut]` tabular attributes — inline string literals keep the table scannable (see [Tabular Attribute Exception](#tabular-attribute-exception))
- **NEVER** use bare `return;` to skip a step in post-deployment mode — use `[SkipStepIf(nameof(ComponentTestSettings.RunAgainstExternalServiceUnderTest), IgnoreReasons.XXX)]` so the reason is documented and the step appears as bypassed in reports (see [test-infrastructure.md](test-infrastructure.md#two-skip-mechanisms))

### ALWAYS
- **ALWAYS** use `partial class` — split scenario definitions from step implementations
- **ALWAYS** inherit from `BaseFixture` (which extends `FeatureFixture` and wraps `WebApplicationFactory<Program>`)
- **ALWAYS** decorate features with `[FeatureDescription]`, scenarios with `[Scenario]`, happy paths with `[HappyPath]`
- **ALWAYS** inject step classes via constructor — resolve via `Get<T>()` on `BaseFixture`
- **ALWAYS** use the three-stage pattern: GIVEN → WHEN → THEN (single cycle per scenario)
- **ALWAYS** include articles (`A`, `An`, `The`) where English grammar requires them
- **ALWAYS** verify both the happy path response AND its downstream interactions
- **ALWAYS** use `X-ComponentTest-RequestId` header for correlating assertions to specific test requests
- **ALWAYS** randomise test entity IDs (`Random.Shared.NextInt64()`, `Guid.NewGuid()`) — no hard-coded values
- **ALWAYS** assert setup (GIVEN) HTTP response status codes
- **ALWAYS** use a **single tabular validation scenario** per endpoint for single-field validations — consolidate all single-field validation cases into one scenario using `[HeadIn]/[HeadOut]/[Inputs]/[Outputs]` attributes with `InvalidFieldFromRequest` and `VerifiableErrorResult`. Cross-field validations get **separate named scenarios** (see [composite-patterns.md](composite-patterns.md#tabular-endpoint-validation-pattern))

## Steps File Conventions

- **Suppress async warnings.** Add `#pragma warning disable CS1998` above the partial class declaration
- **Exactly three regions.** Use `#region Given`, `#region When`, `#region Then` — nothing else
- **Pure delegation.** Steps files are thin orchestration layers. Delegate all logic to reusable step classes via `Get<T>()`. No inline repository access, direct HTTP calls, JSON deserialization, or business logic
  - **Self-contained feature exception.** When a feature's functionality is highly unlikely to be reused by another feature (e.g. OpenAPI spec validation, AsyncAPI spec validation, Scalar UI checks), inline logic is acceptable. Extracting to Common step classes would add unnecessary indirection for code that only one feature will ever use.
- **Resolve step classes in the constructor.** Declare `readonly` fields and assign via `Get<T>()` in the constructor. Use `_camelCase` field names (e.g. `_milkSteps`, `_pancakeSteps`, `_downstreamSteps`).
- **CompositeStep for multi-sub-step Givens.** Return `Task<CompositeStep>` with `Sub.Steps(...)` when orchestrating multiple sub-steps
- **delayAppCreation pattern.** Features using `base(delayAppCreation: true)` must resolve step classes only **after** calling `CreateAppAndClient(overrides)` or `CreateAppAndClient(additionalServices: ...)` (see [composite-patterns.md](composite-patterns.md#config-as-steps-with-lazy-app-creation) and [test-infrastructure.md](test-infrastructure.md#additional-service-registration-additionalservices-callback))

### Steps File Template

```csharp
using BreakfastProvider.Tests.Component.Common.Ingredients;
using BreakfastProvider.Tests.Component.Common.Downstream;
using LightBDD.Framework;

namespace BreakfastProvider.Tests.Component.Scenarios.Pancakes;

#pragma warning disable CS1998
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

    #region Given
    private async Task Milk_is_retrieved()
        => await _milkSteps.Retrieve();
    #endregion

    #region When
    private async Task The_pancakes_are_prepared()
        => await _pancakeSteps.Send();
    #endregion

    #region Then
    private async Task The_cow_service_should_have_received_a_milk_request()
        => _downstreamSteps.AssertCowServiceReceivedMilkRequest();
    #endregion
}
```

## Feature File Conventions

- **Scenario grouping via `#region`** when a feature file has many scenarios
- **Small feature files** (≤ 5 scenarios) do not need regions
- Every feature class **must** have a `[FeatureDescription("...")]` attribute using `Endpoints` constants

## Parallel Safety

All component tests run in parallel (`parallelizeTestCollections: true`). Each feature class is its own xUnit collection.

- `BaseFixture` provides a shared `WebApplicationFactory<Program>` across all features using default config
- Features needing custom config use `delayAppCreation: true` (see [test-infrastructure.md](test-infrastructure.md#delayed-app-creation))
- Each scenario gets unique IDs and data — no shared test data
- In-memory stores and fake services are thread-safe
- Correlate via `X-ComponentTest-RequestId` header (from `BaseEndpoint.RequestId`)
- Never clear shared state as a setup step
- Filter assertions by request ID for parallel-safety

## Component Test Philosophy

- Verify **behaviour through the HTTP layer first** — POST data, then GET it back to assert correctness
- Only fall back to direct assertions when the change cannot be observed via HTTP
- GIVEN steps **must use API calls** rather than direct access whenever an endpoint exists
- Define shared constants in `tests/BreakfastProvider.Tests.Component/Constants/` — endpoint paths, validation messages, ingredient names, topping types, fake service defaults, reusable test data
- Top-level step names must be **business-friendly** — no HTTP status codes at the scenario level; wrap technical details inside `CompositeStep` sub-steps

### Component Tests over Unit Tests

**Prefer component tests over unit tests.** Component tests are the primary testing strategy for this project. Only write unit tests when the code genuinely cannot be exercised through the HTTP layer.

**Why component tests win for this codebase:**
- They test the full pipeline — validation wiring, middleware, DI, serialisation, and behaviour — in one assertion
- A validator unit test can pass while the validator isn't registered; a component test catches both
- The in-process `WebApplicationFactory` is fast enough (~50ms per scenario) that speed isn't a meaningful trade-off
- Tabular BDD scenarios (`[Inputs]`/`[Outputs]`) handle combinatorial validation cases cleanly at the HTTP layer
- Parameterised `[InlineData]` scenarios cover state machine and boundary testing without unit isolation

**When unit tests are justified:**
- Pure utility functions with no HTTP surface (e.g., string helpers, math utilities)
- Code with a very large input space (50+ combinations) where HTTP roundtrips add unnecessary time
- Internal logic that cannot be triggered through any API endpoint

**When unit tests are NOT justified (use component tests instead):**
- FluentValidation validators — test via tabular validation scenarios
- Controller action logic — test through the endpoint
- Middleware behaviour — test via Infrastructure features
- State machine transitions — test via parameterised scenarios
- Service methods called by controllers — test through the controller endpoint

## Stable BDD Specifications

The Specifications YAML is source-controlled. **All values in step names must be deterministic across runs.**

- Store dynamic values in private instance fields; reference them inside parameterless step method bodies
- Only pass compile-time constants, enum values, literals, or **config-derived expressions** as step parameters
- Config-derived values (e.g. `MaxToppings - 1`) are deterministic because they come from appsettings.json

## Constants Over Magic Strings

Prefer constants, `nameof()`, and framework types over inline string literals.

| Instead of | Use | Why |
|---|---|---|
| `"'Milk' is required."` in step code | `PancakeValidationMessages.MilkRequired` | Single source of truth |
| `"application/json"` | `System.Net.Mime.MediaTypeNames.Application.Json` | Framework constant |
| `"Some_Fresh_Milk"` | `CowServiceDefaults.FreshMilk` | Named test constant |
| `"pancakes"` | `Endpoints.Pancakes` | Shared constant |
| `"Raspberries"` | `ToppingDefaults.Raspberries` | Named test constant |
| `"Cow Service"` | `ServiceNames.CowService` | Shared constant |
| `"X-Correlation-Id"` | `CustomHeaders.CorrelationId` | Shared constant |
| `"OrderCreatedEvent"` | `EventTypes.OrderCreated` | Shared constant |
| `"Processed"` | `OutboxStatuses.Processed` | Shared constant |
| `"Healthy"` | `HealthCheckStatuses.Healthy` | Shared constant |
| `"Cow Service Unavailable"` | `DownstreamErrorMessages.CowServiceUnavailableTitle` | Shared constant |
| `"Feature Disabled"` | `DownstreamErrorMessages.FeatureDisabled` | Shared constant |
| `"menu/cache"` | `Endpoints.MenuCache` | Shared constant |
| `"Breakfast Provider is running"` | `Documentation.HeartbeatMessage` | Shared constant |

### Tabular Attribute Exception

Do **not** use constants or `nameof()` inside `[Inputs]`, `[Outputs]`, `[HeadIn]`, or `[HeadOut]` attributes. These attributes form a visual table that is scanned side-by-side — inline string literals keep the table readable and self-documenting.

```csharp
// GOOD — readable table
[Inputs("Milk", "", "Milk is required")][Outputs("'Milk' is required.", "Bad Request")]

// BAD — tabular structure lost in noise
[Inputs(nameof(PancakeRequest.Milk), "", "...")][Outputs(PancakeValidationMessages.MilkRequired, PancakeValidationMessages.BadRequestStatus)]
```

### Available constant classes (`tests/BreakfastProvider.Tests.Component/Constants/`)

| Class | Purpose |
|---|---|
| `Endpoints` | API endpoint path constants (`Pancakes`, `Waffles`, `Orders`, `Milk`, `Eggs`, `Flour`, `GoatMilk`, `Toppings`, `Menu`, `Health`, `Heartbeat`, `MenuCache`) |
| `CustomHeaders` | HTTP header names (`ComponentTestRequestId`, `CorrelationId`) |
| `PancakeValidationMessages` | Server validation error messages and HTTP status descriptions |
| `WaffleValidationMessages` | Server validation error messages for waffles |
| `ToppingValidationMessages` | Server validation error messages (including XSS) for toppings |
| `CowServiceDefaults` | Default fake Cow Service response values |
| `GoatServiceDefaults` | Default fake Goat Service response values |
| `IngredientDefaults` | Default ingredient values (`PlainFlour`, `FreeRangeEggs`, `UnsaltedButter`) |
| `ToppingDefaults` | Standard topping names and categories |
| `MenuDefaults` | Standard menu item names |
| `OrderDefaults` | Order defaults (`PancakeItemType`) |
| `OrderStatuses` | Order state machine states (`Created`, `Preparing`, `Ready`, `Completed`, `Cancelled`) |
| `AuditLogDefaults` | Audit log entity types and actions |
| `ServiceNames` | Named HTTP client identifiers |
| `FakeScenarios` | Fake service scenario values (`ServiceUnavailable`, `Timeout`, `OutOfStock`, `KitchenBusy`) |
| `FakeScenarioHeaders` | Fake service header names for scenario selection |
| `DownstreamErrorMessages` | Error message strings from downstream failure responses (titles, details, feature disabled) |
| `EventTypes` | Event type names (`OrderCreated`) |
| `OutboxStatuses` | Outbox message statuses (`Processed`) |
| `HealthCheckStatuses` | Health check status strings (`Healthy`, `Degraded`, `Unhealthy`) |
| `HealthCheckNames` | Health check dependency names (`CowService`, `GoatService`, `CosmosDb`, `Kafka`) |
| `Documentation` | Service metadata (`ServiceTitle`, `HeartbeatMessage`) |
| `IgnoreReasons` | Post-deployment skip reason strings for `[SkipStepIf]` and `[IgnoreIf]` attributes |

### Style rules

- Prefer `$"{Constant}"` over `Constant.ToString()` for brevity
- Do not use `nameof()` or constants inside `[Inputs]`, `[Outputs]`, `[HeadIn]`, or `[HeadOut]` tabular attributes — see [Tabular Attribute Exception](#tabular-attribute-exception) above
- For enum values in non-attribute code, `$"{HttpStatusCode.BadRequest}"` is acceptable (deterministic)

## TDD Workflow

Follow a **TDD approach** — write or update tests that support each change. Prefer **outside-in component tests**.

When adding a new scenario to an **existing** feature:

1. **Add the scenario method** in the feature `.cs` file — wire GIVEN/WHEN/THEN steps
2. **Add stubs** in the `.steps.cs` file — new step methods that `throw new NotImplementedException()`
3. **Run the test** — confirm it fails with `NotImplementedException` (red)
4. **Implement each step** — replace stubs with real logic, re-running after each (red → green)
5. **Add any downstream assertions** — verify calls to Cow Service, Goat Service, etc.
6. **Run the full feature** — confirm no regressions

When adding a **new feature file**:

1. Create `{Domain}__{Feature_Aspect}_Feature.cs` and `.steps.cs` (partial class pair)
2. Inherit from `BaseFixture`, accept `ITestOutputHelper`
3. Follow the same red → green → downstream assertions workflow above

## Session Completion Checklist

Before ending a coding session or declaring a piece of work complete:

### 0. Endpoint Coverage Checklist

Before declaring test coverage complete for any endpoint, verify the following categories are addressed:

| Category | What to assert | Reference |
|---|---|---|
| **Happy-path response** | Status code, response body fields | [assertion-patterns.md](assertion-patterns.md#response-assertions) |
| **Validation** | Required fields, format constraints | Feature `.cs` tabular attributes or individual scenarios |
| **Downstream calls** | Cow Service, Goat Service, Supplier Service interactions | [assertion-patterns.md](assertion-patterns.md#downstream-assertions) |

### 1. Build

```powershell
dotnet build tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

Fix any compiler errors or warnings before proceeding.

### 2. Run Component Tests

```powershell
dotnet test tests/BreakfastProvider.Tests.Component/BreakfastProvider.Tests.Component.csproj
```

### 3. Interpret Results

- **Failed: 0** — required before completing work
- **Skipped** — acceptable (intentionally skipped tests). Do not count skips as failures.
- **Test count** — if you added N new scenarios, the total should have increased by exactly N

### 4. Record the Baseline

After a verified green run, note the test counts as the new baseline.

## Additional Resources

- **[naming-conventions.md](naming-conventions.md)** — Feature class, scenario, and step method naming rules
- **[composite-patterns.md](composite-patterns.md)** — CompositeStep architecture, config-as-steps, business-meaningful grouping
- **[assertion-patterns.md](assertion-patterns.md)** — Ingredient, topping, recipe, response, and downstream assertion patterns
- **[test-infrastructure.md](test-infrastructure.md)** — InMemory mode, fake services, report generation, CI pipeline
