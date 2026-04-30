# Naming Conventions Reference

All naming rules for feature classes, scenario methods, step methods, and related conventions.

## Feature Class Naming

Feature classes use **Title_Case** with a **double-underscore** separating the domain area from the feature description:

`{Domain}__{Feature_Aspect}_Feature`

| Pattern | Example |
|---|---|
| End-to-end path | `Pancakes__Creation_Feature`, `Orders__Breakfast_Order_Feature` |
| Ingredient variant | `Pancakes__With_Goat_Milk_Feature`, `Waffles__With_Raspberries_Feature` |
| Cross-cutting concern | `Pancakes__Ingredient_Validation_Feature`, `Orders__Topping_Limits_Feature` |
| Config-driven behaviour | `Pancakes__Max_Toppings_Feature`, `Orders__Order_Timeout_Feature` |
| Downstream failure | `Ingredients__Milk_Downstream_Failure_Feature`, `Menu__Downstream_Failure_Feature` |
| Feature flag gating | `Ingredients__Goat_Milk_Feature_Flag_Feature`, `Toppings__Feature_Flag_Feature` |
| Content negotiation | `Pancakes__Content_Negotiation_Feature` |
| XSS / input validation | `Toppings__Xss_Validation_Feature` |
| State transitions | `Orders__Status_Transition_Feature` |
| Lifecycle workflow | `Orders__Complete_Lifecycle_Feature` |
| Deletion / removal | `Toppings__Deletion_Feature` |
| Update / modification | `Toppings__Update_Feature` |
| Pagination | `Orders__Pagination_Feature` |
| Cross-field validation | `Orders__Cross_Field_Validation_Feature` |
| Rate limiting | `Orders__Rate_Limiting_Feature` |
| Caching behaviour | `Menu__Caching_Feature` |
| Infrastructure | `Infrastructure__Health_Check_Feature`, `Infrastructure__Degraded_Health_Check_Feature`, `Infrastructure__Telemetry_Feature` |
| Audit / filtering | `AuditLogs__Filtering_Feature` |

**Rules:**

- The domain prefix (e.g. `Pancakes__`) groups related features together in directory listings and test reports
- Use hierarchical names when a domain area has multiple sub-features: `Creation`, `Topping_Customisation`, `Ingredient_Sourcing`
- Append `_Feature_Flag_Enabled_Feature` when the feature specifically tests behaviour under a feature flag state
- The file pair follows `{ClassName}.cs` and `{ClassName}.steps.cs`

### Feature Description Format

Every feature class **must** have a `[FeatureDescription("...")]` attribute:

```csharp
[FeatureDescription($"/{Endpoints.Pancakes} - Creating pancakes with ingredients and optional toppings")]
```

- Use `Endpoints` constants with `"/"` prefix
- Separate multiple endpoints with `"; /"`
- Use `" - "` between endpoint list and description
- When scenarios run under a feature flag, mention it naturally

## Scenario Naming

Scenario method names **must use Title_Case** — each word capitalised, separated by underscores.

**`Should` is the pivot word** — everything before it describes the action/precondition, everything after describes the expected outcome.

### Scenario Name Structure Templates

| Template | When to use | Examples |
|---|---|---|
| **`{Subject}_Should_{Outcome}`** | Simple subject-driven | `A_Valid_Pancake_Request_Should_Return_A_Fresh_Batch` |
| **`{Action}_With_{Condition}_Should_{Outcome}`** | Action with qualifying condition | `Creating_Pancakes_With_Goat_Milk_Should_Return_A_Specialty_Batch`, `Creating_Waffles_With_Extra_Butter_Should_Return_A_Golden_Batch` |
| **`{Endpoint}_Called_With_{Condition}_Should_{Outcome}`** | Endpoint-centric with input condition | `Pancakes_Endpoint_Called_With_Missing_Flour_Should_Return_A_Bad_Request_Response` |
| **`{Endpoint}_Is_Called_With_{Category}`** | Tabular/validation scenarios | `Pancakes_Endpoint_Is_Called_With_Invalid_Ingredients` |
| **`{Action}_Should_{Behaviour}`** | Behaviour/side-effect focused | `Creating_Pancakes_Should_Source_Milk_From_The_Cow_Service`, `Creating_An_Order_Should_Log_The_Recipe` |
| **`The_{Subject}_Should_{Behaviour}_After_{Trigger}`** | Lifecycle scenarios | `The_Order_Status_Should_Update_After_The_Kitchen_Completes_Preparation` |

**Prefer endpoint-centric phrasing** over caller-centric phrasing:

| Avoid (caller-centric) | Prefer (endpoint-centric) |
|---|---|
| `Caller_Uses_Pancakes_Endpoint_With_...` | `Pancakes_Endpoint_Is_Called_With_...` |
| `User_Orders_Waffles_With_...` | `Waffles_Endpoint_Is_Called_With_...` |
| `Chef_Creates_Order_With_...` | `Order_Endpoint_Is_Called_With_...` |

### Prepositions and Conjunctions

- **`With_`** for conditions: `With_All_Required_Ingredients`, `With_Raspberry_Topping`
- **`And_`** for additional conditions: `And_Maple_Syrup`, `And_A_Table_Number`
- **`Without_`** for negation: `Without_Eggs`, `Without_A_Customer_Name`
- **`When_`** for situational context: `When_The_Cow_Service_Is_Unavailable`, `When_Flour_Is_Out_Of_Stock`
- **`After_`** for temporal triggers: `After_The_Kitchen_Completes_Preparation`, `After_Adding_Extra_Toppings`

### Named Outcomes in Scenario Names

Use descriptive business-friendly outcome phrases, not HTTP status codes:

| Outcome Phrase | Meaning |
|---|---|
| `_Return_A_Fresh_Batch` | 200 OK, pancake batch created |
| `_Return_A_Specialty_Batch` | 200 OK, specialty item with goat milk |
| `_Return_A_Golden_Batch` | 200 OK, waffle batch created |
| `_Return_A_Successful_Response` | 200 OK (generic success) |
| `_Return_A_Bad_Request_Response` | 400 Bad Request |
| `_Return_An_Ingredient_Not_Found_Response` | 404 Not Found |
| `_Return_A_Kitchen_Busy_Response` | 503 Service Unavailable |
| `_Return_An_Internal_Server_Error_Response` | 500 Internal Server Error |
| `_Return_A_Bad_Gateway_Response` | 502 Bad Gateway |
| `_Return_A_Conflict_Response` | 409 Conflict |
| `_Return_An_Unsupported_Media_Type_Response` | 415 Unsupported Media Type |
| `_Return_No_Content` | 204 No Content |
| `_Indicate_Feature_Disabled` | 404 when feature flag is off |
| `_Indicate_A_Bad_Gateway` | 502 downstream error |
| `_Include_All_Ingredients` / `_Include_Raspberries` | Ingredient/topping inclusion outcomes |

## Step Method Naming

Step method names **must use Sentence_Case** — only the first word capitalised:
`The_response_should_include_all_ingredients()`, `A_valid_pancake_request_with_milk_and_eggs()`

### Stage Prefix Convention

Step methods that relate to a specific API endpoint stage use a **stage prefix** to identify the endpoint:

| Stage Prefix | Endpoint | HTTP Method |
|---|---|---|
| `pancakes_` | `/pancakes` (create) | POST |
| `waffles_` | `/waffles` (create) | POST |
| `order_` | `/orders` (create/read/status) | POST/GET/PATCH |
| `topping_` | `/toppings` (add/list/delete) | POST/GET/DELETE |
| `menu_` | `/menu` (list/cache) | GET/DELETE |
| `milk_` | `/milk` (retrieve) | GET |
| `eggs_` | `/eggs` (retrieve) | GET |
| `flour_` | `/flour` (retrieve) | GET |
| `goat_milk_` | `/goat-milk` (retrieve) | GET |
| `health_` | `/health` (check) | GET |
| `heartbeat_` | `/` (heartbeat) | GET |
| `correlation_` | any endpoint (header propagation) | any |
| `audit_` | `/orders` (audit log filtering) | GET |

The stage prefix appears in:
- `The_{stage}_response_should_be_...()` — response assertions
- `The_{stage}_downstream_services_should_have_been_called...()` — downstream assertions
- `The_{stage}_error_should_indicate_...()` — error assertions

> **Scope:** The step method templates below apply to **sub-steps inside CompositeSteps** — these are implementation details not directly visible in the YAML specification. For **top-level** steps that appear in the YAML spec, use domain-centric names instead — see [Domain-Centric Step Naming](#domain-centric-step-naming).

### GIVEN Step Templates

| Category | Template | Examples |
|---|---|---|
| **Valid request** | `A_valid_{endpoint}_request_{qualifier}()` | `A_valid_pancake_request_with_all_ingredients()`, `A_valid_order_request_for_two_pancakes()` |
| **Invalid request** | `A_{endpoint}_request_with_{invalid_qualifier}()` | `A_pancake_request_with_missing_eggs()`, `An_order_request_with_too_many_toppings()` |
| **Prior state** | `A_{thing}_has_been_{past_participle}[_{qualifier}]()` | `A_pancake_batch_has_been_created()`, `An_order_has_been_placed_and_prepared()` |
| **Config value** | `The_{config_name}_is_PARAM(value)` | `The_max_toppings_per_item_is_LIMIT(5)`, `The_order_timeout_is_MINUTES(30)` |
| **Feature flag** | `The_{flag_name}_feature_flag_is_{enabled/disabled}()` | `The_goat_milk_feature_flag_is_enabled()`, `The_raspberry_topping_feature_flag_is_enabled()` |
| **Entity state** | `The_{entity}_is_{state}()` | `The_cow_service_is_available()`, `The_flour_is_out_of_stock()` |
| **Fake service** | `The_{service}_will_return_{scenario}()` | `The_cow_service_will_return_fresh_milk()`, `The_goat_service_will_return_service_unavailable()` |
| **Ingredient setup** | `The_body_specifies_{ingredient}()` | `The_body_specifies_milk()`, `The_body_specifies_raspberries_as_a_topping()` |
| **Capture helper** | `The_{field}_is_captured_from_the_{source}_response()` | `The_milk_is_captured_from_the_milk_endpoint_response()`, `The_batch_id_is_captured_from_the_pancakes_response()` |

### WHEN Step Templates

| Category | Template | Examples |
|---|---|---|
| **Endpoint call** | `The_request_is_sent_to_the_{endpoint}_endpoint()` | `The_request_is_sent_to_the_pancakes_post_endpoint()`, `The_request_is_sent_to_the_orders_post_endpoint()` |
| **Ingredient retrieval** | `{Ingredient}_is_retrieved_from_the_{source}_endpoint()` | `Milk_is_retrieved_from_the_get_milk_endpoint()`, `Goat_milk_is_retrieved_from_the_goat_service()` |
| **Order action** | `The_order_is_{action}[_{qualifier}]()` | `The_order_is_placed()`, `The_order_is_cancelled_before_preparation()` |

### THEN Step Templates

**Response status:**

| Category | Template | Examples |
|---|---|---|
| **Success** | `The_{context}_response_should_be_successful()` | `The_pancakes_response_should_be_successful()`, `The_order_response_should_be_successful()` |
| **Error status** | `The_{context}_response_should_indicate_{error}()` | `The_pancakes_response_should_indicate_a_bad_request()`, `The_order_response_should_indicate_kitchen_busy()` |

**Business-level response composites (top-level steps in scenarios):**

| Category | Template | Examples |
|---|---|---|
| **Successful creation** | `The_{stage}_response_should_contain_a_valid_batch()` | `The_pancakes_response_should_contain_a_valid_batch_with_all_ingredients()` |
| **Ingredient validation** | `The_{stage}_response_should_include_{ingredient}()` | `The_response_ingredients_should_include_milk()`, `The_response_ingredients_should_include_raspberries()` |
| **Topping validation** | `The_{stage}_response_should_include_the_requested_toppings()` | `The_pancakes_response_should_include_the_requested_toppings()` |

**Error composites:**

| Category | Template | Examples |
|---|---|---|
| **Missing ingredient** | `The_{context}_error_should_indicate_a_missing_ingredient()` | `The_pancakes_error_should_indicate_missing_eggs()` |
| **Service unavailable** | `The_{context}_error_should_indicate_{service}_unavailable()` | `The_milk_error_should_indicate_cow_service_unavailable()` |

**Cross-cutting composites:**

| Category | Template | Examples |
|---|---|---|
| **Downstream calls** | `The_{stage}_downstream_services_should_have_been_called[_{qualifier}]()` | `The_pancakes_downstream_services_should_have_been_called_for_milk()` |
| **Recipe logging** | `The_{stage}_recipe_should_have_been_logged()` | `The_order_recipe_should_have_been_logged_with_all_ingredients()` |

### Source-of-Truth Suffixes

When a step asserts a value matches data from a specific source, the step name **must indicate the source**:

| Suffix | Meaning | Example |
|---|---|---|
| `_should_match_the_request()` | Value comes from the test request data | `The_ingredients_should_match_the_request()` |
| `_should_match_the_cow_service_response()` | Value comes from the Cow Service | `The_milk_should_match_the_cow_service_response()` |
| `_should_match_the_goat_service_response()` | Value comes from the Goat Service | `The_goat_milk_should_match_the_goat_service_response()` |
| `_should_match_the_menu_item()` | Value comes from the menu definition | `The_order_item_should_match_the_menu_item()` |

This makes the expected value's origin explicit and self-documenting in the YAML specification.

## Article Grammar

Use correct English articles so the BDD specification reads as natural prose:
- "a" before consonant sounds, "an" before vowel sounds
- Include articles before adjective+noun combinations: `_with_a_missing_ingredient` (not `_with_missing_ingredient`)
- "the" for definite references: `The_pancakes_endpoint_is_called`
- Include "the" before specific field/parameter name references: `_where_the_quantity_exceeds_the_limit` (not `_where_quantity_exceeds_limit`)
- Include "the" before each noun in compound noun phrases joined by "and": `_with_the_milk_and_the_flour` (not `_with_the_milk_and_flour`)
- Endpoint-centric scenario names **must** start with the endpoint name: `Pancakes_Endpoint_Is_Called_With_...`
- Avoid contractions — apostrophes cannot appear in method names. Rephrase instead: `_with_a_missing_field` (not `_with_a_field_thats_missing`)

## Avoid Acronyms

Expand acronyms to keep step names readable. Keep well-known food terms as-is (e.g. `milk`, `flour`).

## LightBDD Parameter Placement in Step Names

LightBDD formats parameterised step names using three placement rules (in priority order):

1. **CAPITAL LETTERS** — If the method name contains the parameter name in UPPERCASE, the formatted value replaces that word:
   `The_max_toppings_per_item_is_LIMIT(int limit)` → `The max toppings per item is "5"`
2. **Variable name match** — If a word in the method name matches the parameter name (case-insensitive), the value is placed after that word:
   `Product_is_in_stock(string product)` → `Product "flour" is in stock`
3. **End placement** — Otherwise, the value is appended as `[paramName: "value"]`:
   `The_response_should_be(string expectedStatus)` → `The response should be [expectedStatus: "OK"]`

**Rules:**

- **Prefer CAPITAL LETTERS placement** when the parameter should appear at a specific position to form natural English
- **Avoid accidental variable-name matches.** If a parameter name matches a word already in the method name, LightBDD will place the value mid-sentence. Use CAPITAL LETTERS instead.
- **Match the parameter name to the UPPERCASE word** — `_LIMIT` in the method name requires the parameter to be named `limit` (case-insensitive match)

```csharp
// ✅ Natural English: "The max toppings per item is "5""
private async Task The_max_toppings_per_item_is_LIMIT(int limit) { ... }

// ❌ Bracket suffix: "The max toppings per item is [expectedLimit: "5"]"
private async Task The_max_toppings_per_item_is(int expectedLimit) { ... }
```

## Feature Flag Naming Convention

When configuring a feature flag via config overrides, use:
`The_{featureFlagName}_feature_flag_is_{enabled/disabled}[_with_params]()`

| Config key | Step method name |
|---|---|
| `FeatureSwitchesConfig:IsGoatMilkEnabled` = true | `The_goat_milk_feature_flag_is_enabled()` |
| `FeatureSwitchesConfig:IsRaspberryToppingEnabled` = true | `The_raspberry_topping_feature_flag_is_enabled()` |
| `ToppingRulesConfig:MaxToppingsPerItem` = 5 | `The_max_toppings_per_item_is_LIMIT(5)` |
| `OrderRulesConfig:OrderTimeoutMinutes` = 30 | `The_order_timeout_is_MINUTES(30)` |

## Descriptive Assertion Step Names

Step names in the YAML specification **must describe the business outcome**, not use generic phrases.

**Banned patterns (never use in any step name — top-level or sub-step):**

- `*_expected_fields()` / `*_expected_fields` — e.g. ~~`The_response_should_have_the_expected_fields()`~~
- `*_fields_should_match()`
- `*_should_be_as_expected()`
- `*_should_have_expected_values()`

**Instead, name what is being verified and its source of truth:**

| Bad (generic) | Good (descriptive) |
|---|---|
| `The_response_should_have_the_expected_fields()` | `The_response_ingredients_should_include_milk()` |
| `The_response_fields_should_match()` | `The_pancakes_response_should_contain_a_valid_batch_with_all_ingredients()` |
| `The_downstream_should_have_been_called_as_expected()` | `The_cow_service_should_have_received_a_milk_request()` |

For **composite steps** that group multiple assertions, the composite name **must describe the business context** (e.g. `_with_all_ingredients_and_toppings()`, `_from_the_cow_service()`) — never `_with_the_expected_fields()`.

## Business-Friendly Top-Level Step Names

Top-level steps **must be written for a non-technical audience**. No HTTP status codes or JSON at the scenario level.

| Technical (avoid) | Business-friendly (preferred) |
|---|---|
| `The_response_status_code_should_be(HttpStatusCode.OK)` | `The_response_should_be_successful()` |
| `The_response_should_have_status_400()` | `The_pancakes_error_should_indicate_missing_eggs()` |

Wrap technical details inside `CompositeStep` sub-steps.

## Domain-Centric Step Naming

> **Scope:** This section defines naming for **top-level** steps that appear in the YAML specification. The endpoint-centric templates in the [Step Method Naming](#step-method-naming) section above are appropriate for **sub-steps** inside CompositeSteps, where technical detail is expected.

Top-level step names **must describe what is happening in the domain**, not the mechanics of the test or the endpoint being called.

### Avoid Mechanic-Centric Names

> The "Avoid" column shows names that should **not** appear as top-level steps. These names are acceptable as sub-step names inside CompositeSteps.

| Stage | Avoid (mechanic-centric) | Prefer (domain-centric) |
|---|---|---|
| GIVEN | `A_valid_post_request_body()` | `A_valid_pancake_recipe_with_all_ingredients()` |
| GIVEN | `The_http_client_calls_the_cow_service()` | `Milk_is_sourced_from_the_cow_service()` |
| WHEN | `The_post_endpoint_is_called()` | `The_pancakes_are_prepared()` |
| WHEN | `The_get_endpoint_is_called()` | `The_menu_is_requested()` |
| THEN | `The_response_should_contain_the_requested_items()` | `The_breakfast_order_should_contain_all_items()` |
| THEN | `The_response_body_should_have_ingredients()` | `The_pancake_batch_should_include_all_ingredients()` |

### Domain-State Rules by Stage

- **GIVEN** — Describe the **domain state** that exists, not how the test created it. Use past-tense or existential phrasing: `Milk_has_been_sourced()`, `A_valid_pancake_recipe_with_all_ingredients()`.
- **WHEN** — Describe the **domain action** being performed, not the HTTP call: `The_pancakes_are_prepared()`, `The_breakfast_order_is_placed()`.
- **THEN** — Describe the **domain outcome**, not the response mechanics: `The_pancake_batch_should_include_all_ingredients()`, `The_order_should_contain_raspberries()`.

### Singular vs Plural Consistency

Match the grammatical number of GIVEN and THEN steps within a scenario:

| GIVEN creates… | GIVEN name | THEN name |
|---|---|---|
| One batch | `A_pancake_batch_has_been_created()` | `The_response_should_contain_the_batch()` |
| Multiple items | `Breakfast_items_have_been_ordered()` | `The_response_should_contain_the_ordered_items()` |

### Filtering Tests — Prove Exclusion, Not Just Inclusion

When testing that an endpoint filters by a field (e.g. order ID, batch ID, topping type), **write data for multiple distinct values** and assert the response contains **only** records matching the queried value.

**Pattern:**

```
GIVEN {Records for different {field} values exist}
  ├── {A pancake batch exists with blueberry toppings}
  ├── {A second pancake batch exists with blueberry toppings}
  └── {A waffle batch exists with maple syrup topping}
WHEN  {The batches are requested for blueberry topping}
THEN  {The response should only contain the blueberry-topped batches}
  ├── response is successful
  ├── response has exactly 2 records
  ├── all records have blueberry topping
  └── all records have a batch ID
```

## Reusable Step Class Naming

Reusable step classes in `Common/{Feature}/` follow these naming conventions:

| Pattern | Purpose | Examples |
|---|---|---|
| `{Http}{Endpoint}Steps` | HTTP endpoint interaction (setup + call + basic assertions) | `PostPancakesSteps`, `GetMilkSteps`, `PostOrderSteps`, `GetMenuSteps` |
| `{Domain}IngredientSteps` | Ingredient validation assertions | `PancakeIngredientSteps` |
| `{Domain}ToppingSteps` | Topping validation assertions | `PancakeToppingSteps` |
| `DownstreamRequestSteps` | HTTP request assertions for outbound calls to dependencies | `DownstreamRequestSteps` |

**Rules:**

- HTTP step classes inherit from `BasePostEndpoint<TRequest, TResponse>` or `BaseGetEndpoint<TResponse>` as appropriate
- Non-HTTP step classes are plain classes (no base class) — they use `Sub.Steps(...)` for composites (same as feature steps files)
- All step classes are registered as transient via DI and resolved via `Get<T>()` in feature step files
- Step classes own the low-level mechanics; feature step files own the business-level orchestration

## Expected Property Injection Pattern

Reusable step classes use **`Expected{FieldName}`** properties for injecting expected values before running assertions:

```csharp
// In the step class
public string? ExpectedMilk { get; set; }
public string? ExpectedEggs { get; set; }
public string? ExpectedFlour { get; set; }
```

Feature step files populate these via a `SetCommon{Domain}ExpectedValues()` helper:

```csharp
private void SetCommonIngredientExpectedValues()
{
    _pancakeIngredientSteps.ExpectedMilk = _milkResponse.Milk;
    _pancakeIngredientSteps.ExpectedEggs = _eggsResponse.Eggs;
    _pancakeIngredientSteps.ExpectedFlour = _flourResponse.Flour;
}
```

**Rules:**

- Use `Expected{FieldName}` (PascalCase, no underscores) for the property name
- Create one `SetCommon{Domain}ExpectedValues()` helper per cross-cutting step class
- Call the helper at the start of the composite step that uses the step class

## Boundary-Obvious Test Values

**All test data values must be obviously relatable to the config limits they exercise.** Use config-derived expressions so tests adapt to config changes.

| Intent | Bad (arbitrary) | Good (config-derived) | Why |
|---|---|---|---|
| Under topping limit | `3` | `MaxToppings - 1` | Clearly below — adapts to config |
| Over topping limit | `10` | `MaxToppings + 1` | Clearly above — adapts to config |
| At the limit exactly | `5` | `MaxToppings` | Tests the boundary itself |
| Under order timeout | `15` | `OrderTimeoutMinutes / 2` | Safe under the timeout |

This applies to **every** quantity in the test — topping counts, order sizes, and timeout durations.
