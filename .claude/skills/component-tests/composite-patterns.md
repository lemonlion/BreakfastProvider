# CompositeStep Patterns Reference

Architecture, rules, and examples for `CompositeStep` usage including ingredient grouping, config-as-steps, business-meaningful grouping, and the downstream assertion pattern.

## CompositeStep Rules

Use `CompositeStep` to expose nested detail in the LightBDD HTML specification report.

**Convert to CompositeStep when:**
- 2+ ANDs in the same clause that can be collapsed
- 4+ related assertions on the same response
- Steps that perform an HTTP call + assertions
- Given steps with multiple phases or action + assertion

**Do NOT convert when:**
- Single-delegation wrappers (would just repeat the parent name)
- Pure request-preparation methods (no endpoint calls or assertions)

### Sub.Steps and StartWith.SubSteps

`Sub` and `StartWith` are static helper classes in `Tests.Component.Util` that wrap `CompositeStep.DefineNew()` internally. **Always use these helpers** — never call `CompositeStep.DefineNew()` directly.

| Helper | Returns | Use when |
|---|---|---|
| `Sub.Steps(...)` | `CompositeStep` (built) | Returning from `async Task<CompositeStep>` methods — the common case |
| `StartWith.SubSteps(...)` | `ICompositeStepBuilder<NoContext>` (unbuild) | Chaining additional steps before calling `.Build()` |

Both are available **everywhere** — feature steps files, standalone step classes, and infrastructure helpers alike.

Always declare `async Task<CompositeStep>` and `return Sub.Steps(...)` — never `CompositeStep.DefineNew()` directly or `Task.FromResult(...)`.

## State Capture as Sub-Step

When a CompositeStep needs to capture state for later steps, make the capture itself a named sub-step:

```csharp
private async Task<CompositeStep> A_pancake_batch_has_been_created()
{
    return Sub.Steps(
        _ => The_request_is_sent_to_the_pancakes_post_endpoint(),
        _ => The_pancakes_response_should_be_successful(),
        _ => The_batch_id_is_captured_from_the_pancakes_response());
}

private async Task The_batch_id_is_captured_from_the_pancakes_response()
{
    _batchId = _pancakeResponse!.BatchId;
}
```

Use naming convention `The_{field}_is_captured_from_the_{source}_response()` for capture helpers.

## Mutable Field Pattern

Store dynamic values in private instance fields and reference them inside parameterless step method bodies. This keeps the BDD specification stable:

```csharp
private Guid _batchId;
private string _customerName = null!;
private MilkResponse _milkResponse = new();

// Good — field referenced inside the step body, not passed as a parameter
private async Task A_valid_order_request_for_the_created_batch()
{
    _orderRequest.BatchId = _batchId;
}

// Bad — dynamic value leaks into the step name and breaks YAML stability
private async Task A_valid_order_request_for_batch(Guid batchId) { ... }
```

## Single GIVEN/WHEN/THEN Structure

Each scenario **must** follow a single GIVEN/WHEN/THEN structure. No multiple WHEN/THEN cycles. Prerequisites can be expressed as a single GIVEN composite step, or as multiple steps using `and =>` / `but =>` continuations before the WHEN:

```csharp
// GIVEN — single composite step collapsing all prerequisites
await Runner.RunScenarioAsync(
    given => A_valid_post_request_for_the_pancakes_endpoint(),
    when => The_request_is_sent_to_the_pancakes_post_endpoint(),
    then => The_pancakes_response_should_be_successful());

// GIVEN-AND-WHEN — multiple additive prerequisites before the action
await Runner.RunScenarioAsync(
    given => The_max_toppings_per_item_is_LIMIT(MaxToppings),
    and => A_valid_waffle_recipe_with_all_ingredients(),
    and => The_request_has_more_toppings_than_the_configured_limit(),
    when => The_waffles_are_prepared(),
    then => The_waffles_response_should_indicate_too_many_toppings());

// GIVEN-BUT-WHEN — contrasting prerequisite highlights a deliberate override or exception
await Runner.RunScenarioAsync(
    given => A_valid_pancake_request_with_all_ingredients(),
    but => The_cow_service_is_unavailable(),
    when => The_request_is_sent_to_the_pancakes_post_endpoint(),
    then => The_response_should_indicate_a_downstream_service_error());
```

Use `and =>` when each prerequisite **adds** to the setup. Use `but =>` when a prerequisite **contrasts** with the preceding context (e.g. a happy-path setup followed by a service being unavailable). Both are valid in the GIVEN clause and keep the YAML specification readable.

## Ingredient Grouping Pattern

Group ingredient-related steps into business-meaningful composites:

```csharp
private async Task<CompositeStep> A_valid_post_request_for_the_pancakes_endpoint()
{
    return Sub.Steps(
        _ => A_valid_request_body());
}

private async Task<CompositeStep> A_valid_request_body()
{
    return Sub.Steps(
        _ => The_body_specifies_milk(),
        _ => The_body_specifies_eggs(),
        _ => The_body_specifies_flour());
}

private async Task<CompositeStep> The_body_specifies_milk()
{
    return Sub.Steps(
        _ => Milk_is_retrieved_from_the_get_milk_endpoint(),
        _ => The_milk_response_should_be_successful(),
        _ => Retrieved_milk_is_set_on_the_body());
}
```

### Topping Addition Pattern

When toppings are optional, they follow a similar grouping pattern:

```csharp
private async Task<CompositeStep> A_valid_pancake_request_with_raspberries()
{
    return Sub.Steps(
        _ => A_valid_request_body(),
        _ => The_body_specifies_raspberries_as_a_topping());
}

private async Task<CompositeStep> The_body_specifies_raspberries_as_a_topping()
{
    return Sub.Steps(
        _ => Raspberries_are_retrieved_from_the_toppings_endpoint(),
        _ => Retrieved_raspberries_are_added_to_the_body());
}
```

### Multiple Milk Source Pattern

When different recipes use different milk sources:

```csharp
private async Task<CompositeStep> The_body_specifies_cow_milk()
{
    return Sub.Steps(
        _ => Milk_is_retrieved_from_the_cow_service(),
        _ => Retrieved_milk_is_set_on_the_body());
}

private async Task<CompositeStep> The_body_specifies_goat_milk()
{
    return Sub.Steps(
        _ => Goat_milk_is_retrieved_from_the_goat_service(),
        _ => Retrieved_goat_milk_is_set_on_the_body());
}
```

## Downstream Assertion Pattern

Every scenario that calls an external service ends with downstream assertion steps:

**Happy-path scenarios:**

```csharp
// When the scenario sources milk from the Cow Service:
and => The_cow_service_should_have_received_a_milk_request(),
// When the scenario sources goat milk from the Goat Service:
and => The_goat_service_should_have_received_a_goat_milk_request(),
// When the scenario checks ingredient availability:
and => The_supplier_service_should_have_received_an_availability_check(),
```

**Error scenarios (service unavailable):**
- Assert only the error status code and message; downstream may or may not have been called depending on when the failure occurred

## Config-as-Steps with Lazy App Creation

When a feature uses `delayAppCreation: true` and tests behaviour driven by **multiple config values**, surface each config value as a separate explicit GIVEN step. This makes the config visible in the YAML specification and keeps tests self-documenting.

### Pattern

1. Accumulate config overrides into a `Dictionary<string, string?>` field across multiple GIVEN steps
2. Use an `EnsureAppCreated()` helper that calls `CreateAppAndClient(_configOverrides)` on first call
3. Call `EnsureAppCreated()` at the start of the first step that needs the app

> **Note:** Step classes resolved via `Get<T>()` in the constructor already use deferred `Func<HttpClient>` through `RequestContext`, so they work correctly even when `CreateAppAndClient()` is called later. The `() => Client` lambda captures the property, not its initial value.

```csharp
private readonly Dictionary<string, string?> _configOverrides = new();
private bool _appCreated;

private void EnsureAppCreated()
{
    if (_appCreated) return;
    CreateAppAndClient(_configOverrides);
    _appCreated = true;
}

// Each config GIVEN step adds to the dictionary (no app creation yet)
private async Task The_goat_milk_feature_flag_is_enabled()
{
    _configOverrides["FeatureSwitchesConfig:IsGoatMilkEnabled"] = "true";
}

private async Task The_max_toppings_per_item_is_LIMIT(int limit)
{
    _configOverrides["ToppingRulesConfig:MaxToppingsPerItem"] = limit.ToString();
}

// First step needing the app triggers lazy creation
private async Task A_valid_pancake_request_with_all_ingredients()
{
    EnsureAppCreated();
    // ... prepare request
}
```

**Resulting scenario reads naturally with all config values visible:**

```csharp
await Runner.RunScenarioAsync(
    given => The_goat_milk_feature_flag_is_enabled(),
    and => The_max_toppings_per_item_is_LIMIT(5),
    and => A_valid_pancake_request_with_goat_milk_and_raspberries(),
    when => The_request_is_sent_to_the_pancakes_post_endpoint(),
    then => The_response_should_be_successful());
```

### Guidelines

- Each config step should set exactly **one** config key
- The feature flag step (e.g. `_is_enabled()`) sets the mode; limit steps set the specific thresholds

### When to Use Which Pattern

| Pattern | Use when |
|---|---|
| **Config-as-Steps** (preferred for overrides) | Feature uses `delayAppCreation: true` — steps both set config AND surface values |
| **Config-from-App** (preferred for defaults) | Feature does NOT override config — read live values via `AppFactory.Services` and compute inputs/outputs from them |
| **Informational top-level steps** | At the **top level** of each scenario (not inside composites), to surface config values in the YAML spec |

### Feature Flag Testing with Delayed App Creation

Feature flag tests use `delayAppCreation: true` to override `FeatureSwitchesConfig` values. Each scenario creates a fresh app with the desired flag state:

```csharp
public partial class Ingredients__Goat_Milk_Feature_Flag_Feature : BaseFixture
{
    public Ingredients__Goat_Milk_Feature_Flag_Feature() : base(delayAppCreation: true) { }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest),
        IgnoreReasons.NeedsNonDefaultConfiguration)]
    public async Task Goat_Milk_Endpoint_Should_Return_Successfully_When_Feature_Is_Enabled()
    {
        await Runner.RunScenarioAsync(
            given => The_goat_milk_feature_flag_is_enabled(),
            when => The_goat_milk_endpoint_is_called(),
            then => The_response_should_be_successful());
    }
}
```

Key considerations:
- Always use `[IgnoreIf(..., IgnoreReasons.NeedsNonDefaultConfiguration)]` since deployed services have fixed config
- Config keys follow the pattern `{SectionName}:{PropertyName}` (e.g. `FeatureSwitchesConfig:IsGoatMilkEnabled`)
- Step classes resolved in the constructor work correctly with late `CreateAppAndClient()` thanks to `Func<HttpClient>` in `RequestContext`

## Config-from-App Pattern

When a feature uses **default config** (no `delayAppCreation`), read config values from the running application's DI container and use them to compute test inputs and expected outputs.

### Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

private ToppingRulesConfig? _toppingRules;
private ToppingRulesConfig ToppingRules => _toppingRules ??=
    AppFactory.Services.GetRequiredService<IOptions<ToppingRulesConfig>>().Value;

private int MaxToppings => ToppingRules.MaxToppingsPerItem;
```

### Using Config-Derived Values in Scenarios

```csharp
// Input quantities relative to config limits
given => A_valid_pancake_request_with_TOPPING_COUNT_toppings(MaxToppings - 1),

// Expected outputs computed from config
then => The_response_should_indicate_COUNT_toppings_remaining(MaxToppings),
```

### Rules

- Config-derived expressions (`MaxToppings - 1`) are deterministic and safe for BDD specs
- Use `IOptions<T>` with lazy loading (`??=`) to avoid accessing services before the app host is built
- Keep informational config steps at the **top level** of each scenario
- **Surface the quantity in the step name** so the reader can see how it relates to the stated limits

## Grouping Repetitive Assertions into Business-Meaningful Composites

When a CompositeStep contains many (4+) related individual assertion sub-steps that always appear together, group them into a higher-level composite with a **business-meaningful name**.

### Response Assertion Composites

```csharp
private async Task<CompositeStep> The_response_should_be_successful()
{
    return Sub.Steps(
        _ => The_response_http_status_should_be_ok(),
        _ => The_response_should_be_valid_json(),
        _ => The_response_ingredients_should_include_milk(),
        _ => The_response_ingredients_should_include_eggs(),
        _ => The_response_ingredients_should_include_flour());
}
```

### Topping Assertion Composites

```csharp
private async Task<CompositeStep> The_response_should_include_all_requested_toppings()
{
    return Sub.Steps(
        _ => The_response_toppings_should_include_raspberries(),
        _ => The_response_toppings_should_include_blueberries(),
        _ => The_response_toppings_should_include_maple_syrup());
}
```

### Order Validation Composites

```csharp
private async Task<CompositeStep> The_order_response_should_contain_a_complete_order()
{
    return Sub.Steps(
        _ => The_order_response_should_have_an_order_id(),
        _ => The_order_response_should_have_a_status_of_pending(),
        _ => The_order_response_should_contain_all_ordered_items(),
        _ => The_order_response_should_have_an_estimated_ready_time());
}
```

### Guidelines

- **Always use the grouped composites** for multi-field response assertions — do not list individual fields inline
- Group by **business domain** (ingredient validation, topping validation, order details) — not by technical similarity
- Keep **feature-specific assertions** (e.g. specialty toppings, custom milk source) as individual steps at the feature level
- Name composites to describe **what** is being verified, including the **source of truth**: `_from_the_cow_service()`, `_from_the_request()`

## Response Field Assertions

THEN steps **must assert all fields** of HTTP responses from the WHEN section:
- **200 OK**: Assert all always-present fields *and* nullable fields (BeNull when not expected)
- **400**: Assert status code + validation error message
- **404**: Assert status code + not found details
- **500**: Assert only the status code
- **204**: Assert empty body

## Tabular Endpoint Validation Pattern

**Every endpoint must have exactly one validation scenario** that exercises all invalid-input cases as a data-driven table. Never create separate scenarios for individual validation failures.

### Feature File Pattern

```csharp
[Scenario]
[HeadIn("Field",  "Value", "Reason"                          )][HeadOut("Error Message",              "Response Status")]
[Inputs("Milk",   "",      "Milk is required"                  )][Outputs("'Milk' is required.",         "Bad Request"    )]
[Inputs("Flour",  "",      "Flour is required"                 )][Outputs("'Flour' is required.",        "Bad Request"    )]
[Inputs("Eggs",   "",      "Eggs is required"                  )][Outputs("'Eggs' is required.",         "Bad Request"    )]
[Inputs("Eggs",   "<script>alert('xss')</script>", "XSS input")][Outputs("'Eggs' contains invalid characters.", "Bad Request")]
public async Task Pancakes_Endpoint_Is_Called_With_Invalid_Ingredients_Should_Return_A_Bad_Request_Response()
{
    await Runner.RunScenarioAsync(
        given => Valid_pancake_requests_with_a_field_thats_invalid(TableFrom.Inputs<InvalidFieldFromRequest>()),
        when => The_validation_requests_are_sent_to_the_pancakes_endpoint(),
        then => The_responses_should_each_contain_the_validation_error_for_the_invalid_field(VerifiableTableFrom.Outputs<VerifiableErrorResult>()));
}
```

### What to Include in the Table

Cover validation rules for **single field in isolation**:

| Category | Examples |
|---|---|
| **Required fields** | Empty strings, null objects |
| **Format constraints** | Wrong length, invalid characters, out-of-range values, negative quantities |
| **XSS protection** | `<script>`, `<a>` injections (when the validator checks for XSS) |

### Cross-Field Validation — Separate Scenarios

Validation rules that involve **relationships between two or more fields** do NOT fit the tabular pattern:

**Examples of cross-field rules:**

| Rule | Why it doesn't fit the table |
|---|---|
| `Topping count must not exceed MaxToppings` when multiple toppings are specified | Requires setting multiple topping fields |
| `Goat milk requires the goat milk feature flag to be enabled` | Requires config interaction, not just request mutation |
| `Waffle butter quantity must not exceed flour quantity` | Tests a constraint across two fields |

**Pattern for cross-field validation scenarios:**

```csharp
[Scenario]
public async Task A_Pancake_Request_With_More_Toppings_Than_Allowed_Should_Return_A_Bad_Request()
{
    await Runner.RunScenarioAsync(
        given => The_max_toppings_per_item_is_LIMIT(3),
        and => A_pancake_request_with_COUNT_toppings(4),
        when => The_request_is_sent_to_the_pancakes_post_endpoint(),
        then => The_response_should_indicate_too_many_toppings());
}
```

### Reason Column Text

The `Reason` column in `[Inputs]` must describe **the validation rule being tested**, not the test setup state.

| Bad (describes setup) | Good (describes the rule) |
|---|---|
| `"Missing field"` | `"Milk is required"` |
| `"Empty"` | `"Flour must not be empty"` |
| `"Bad value"` | `"Quantity must be greater than zero"` |

### Rules

- **One tabular scenario per endpoint** for single-field validations — never split into multiple scenarios
- **Separate scenarios for cross-field validations** — rules involving relationships between fields get their own named scenario
- **Include the `Reason` column** in `[Inputs]` to explain the validation rule being tested
- **Use `"Bad Request"` as the default** `ResponseStatus` — only override for non-400 validation errors
- **Match error messages exactly** as returned by the validators
