# Assertion Patterns Reference

Patterns for all assertion types in component tests: response, ingredient validation, topping validation, recipe logging, and downstream service requests.

## Response Assertions

### Success Responses (200 OK)

Assert all always-present fields *and* nullable fields (BeNull when not expected). Use business-meaningful composite names at the top level:

```csharp
// Top-level step — business-friendly
then => The_response_should_be_successful(),

// Inside the composite — technical details
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

### Error Responses

| Status | What to assert |
|---|---|
| **400** | Status code + validation error message(s) |
| **404** | Status code + not found details |
| **409** | Status code + conflict reason (e.g. invalid state transition) |
| **415** | Status code only (unsupported media type) |
| **500** | Status code only |
| **502** | Status code + downstream error message |
| **503** | Status code + service unavailable message |

Error composites wrap detail checks:

```csharp
private async Task<CompositeStep> The_pancakes_error_should_indicate_missing_eggs()
{
    return Sub.Steps(
        _ => The_response_http_status_should_be_bad_request(),
        _ => The_error_message_should_contain("'Eggs' is required."));
}
```

### Bad Gateway (502) Assertions

When a downstream service returns an error, the API returns 502 Bad Gateway:

```csharp
private async Task The_response_should_indicate_a_bad_gateway()
{
    _getMilkSteps.Response!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
}

private async Task The_error_should_describe_the_downstream_failure()
{
    var content = await _getMilkSteps.Response!.Content.ReadAsStringAsync();
    content.Should().Contain(DownstreamErrorMessages.CowServiceUnavailable);
}
```

### Conflict (409) Assertions

When an invalid state transition is attempted (e.g. order status):

```csharp
private async Task The_response_should_indicate_a_conflict()
{
    _patchOrderStatusSteps.Response!.StatusCode.Should().Be(HttpStatusCode.Conflict);
}
```

### Unsupported Media Type (415) Assertions

Content negotiation — send unsupported Accept headers:

```csharp
private async Task The_response_should_indicate_unsupported_media_type()
{
    _postPancakesSteps.Response!.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
}
```

### No Content (204) Assertions

Successful deletion or cache clearing:

```csharp
private async Task The_response_should_indicate_no_content()
{
    _deleteToppingSteps.Response!.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

### Feature Disabled (404) Assertions

When a feature flag is off, the endpoint returns 404:

```csharp
private async Task The_response_should_indicate_feature_disabled()
{
    _getGoatMilkSteps.Response!.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

### Correlation ID Header Assertions

Verify the X-Correlation-Id header is present and matches expectations:

```csharp
private async Task The_response_should_echo_the_correlation_id()
{
    _response!.Headers.GetValues("X-Correlation-Id").Should().Contain(_sentCorrelationId);
}

private async Task The_response_should_include_an_auto_generated_correlation_id()
{
    _response!.Headers.Contains("X-Correlation-Id").Should().BeTrue();
    var id = _response.Headers.GetValues("X-Correlation-Id").First();
    Guid.TryParse(id, out _).Should().BeTrue();
}
```

### Setup (GIVEN) Response Assertions

When a GIVEN step makes an HTTP call, the response **must be verified** inside composite sub-steps:

```csharp
private async Task<CompositeStep> The_body_specifies_milk()
{
    return Sub.Steps(
        _ => Milk_is_retrieved_from_the_get_milk_endpoint(),
        _ => The_milk_response_should_be_successful(),
        _ => Retrieved_milk_is_set_on_the_body());
}

private async Task Milk_is_retrieved_from_the_get_milk_endpoint()
{
    _milkHttpResponse = await Client.GetAsync("milk");
}

private async Task The_milk_response_should_be_successful()
{
    _milkHttpResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    _milkResponse = (await _milkHttpResponse.Content.ReadFromJsonAsync<MilkResponse>())!;
}

private async Task Retrieved_milk_is_set_on_the_body()
{
    _pancakeRequest.Milk = _milkResponse.Milk;
}
```

## Ingredient Assertions

### Pancake Ingredient Composites

```csharp
private async Task<CompositeStep> The_pancake_batch_should_include_all_ingredients()
{
    return Sub.Steps(
        _ => The_response_ingredients_should_include_milk(),
        _ => The_response_ingredients_should_include_eggs(),
        _ => The_response_ingredients_should_include_flour());
}

private async Task The_response_ingredients_should_include_milk()
    => _pancakeResponse!.Ingredients.Should().Contain(_milkResponse.Milk);

private async Task The_response_ingredients_should_include_eggs()
    => _pancakeResponse!.Ingredients.Should().Contain(_eggsResponse.Eggs);

private async Task The_response_ingredients_should_include_flour()
    => _pancakeResponse!.Ingredients.Should().Contain(_flourResponse.Flour);
```

### Waffle Ingredient Composites

```csharp
private async Task<CompositeStep> The_waffle_batch_should_include_all_ingredients()
{
    return Sub.Steps(
        _ => The_response_ingredients_should_include_milk(),
        _ => The_response_ingredients_should_include_eggs(),
        _ => The_response_ingredients_should_include_flour(),
        _ => The_response_ingredients_should_include_butter());
}
```

### Specialty Ingredient Assertions

When using non-standard ingredient sources (e.g. goat milk instead of cow milk):

```csharp
private async Task The_response_ingredients_should_include_goat_milk()
    => _pancakeResponse!.Ingredients.Should().Contain(_goatMilkResponse.GoatMilk);

private async Task The_response_milk_should_match_the_goat_service_response()
    => _pancakeResponse!.Ingredients
        .Should().Contain(i => i == _goatMilkResponse.GoatMilk);
```

## Topping Assertions

### Topping Presence Composites

```csharp
private async Task<CompositeStep> The_response_should_include_all_requested_toppings()
{
    return Sub.Steps(
        _ => The_response_toppings_should_include_raspberries(),
        _ => The_response_toppings_should_include_blueberries(),
        _ => The_response_toppings_should_include_whipped_cream());
}

private async Task The_response_toppings_should_include_raspberries()
    => _pancakeResponse!.Toppings.Should().Contain("Raspberries");

private async Task The_response_toppings_should_include_blueberries()
    => _pancakeResponse!.Toppings.Should().Contain("Blueberries");

private async Task The_response_toppings_should_include_whipped_cream()
    => _pancakeResponse!.Toppings.Should().Contain("Whipped Cream");
```

### Topping Count Assertions

```csharp
private async Task The_response_should_contain_COUNT_toppings(int expectedCount)
    => _pancakeResponse!.Toppings.Should().HaveCount(expectedCount);

private async Task The_response_should_contain_no_toppings()
    => _pancakeResponse!.Toppings.Should().BeEmpty();
```

### Topping Exclusion Assertions

When a feature flag disables a topping, assert it is absent from the response:

```csharp
private async Task The_response_should_not_include_raspberries()
    => toppingNames.Should().NotContain("Raspberries");

private async Task The_response_should_include_all_other_toppings()
    => toppingNames.Should().Contain("Blueberries").And.Contain("Maple Syrup");
```

### Topping Limit Assertions

```csharp
private async Task<CompositeStep> The_response_should_indicate_too_many_toppings()
{
    return Sub.Steps(
        _ => The_response_http_status_should_be_bad_request(),
        _ => The_error_message_should_contain("Maximum toppings exceeded"));
}
```

## Downstream Service Assertions

Component tests **must verify HTTP requests** sent to downstream dependencies when those interactions occur.

> **Post-deployment mode:** Downstream assertions depend on `FakeRequestStore` which is unavailable in post-deployment mode. Decorate these step methods with `[SkipStepIf(nameof(ComponentTestSettings.RunAgainstExternalServiceUnderTest), IgnoreReasons.DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]`. The step will appear as bypassed in LightBDD reports. See [test-infrastructure.md](test-infrastructure.md#two-skip-mechanisms) for the full pattern.

### Cow Service Assertions

Verify that the Cow Service was called when milk is sourced. Use `FakeRequestStore` keyed by the test's `X-ComponentTest-RequestId` header:

```csharp
private async Task The_cow_service_should_have_received_a_milk_request()
{
    var requests = _fakeRequestStore.GetRequests(_requestId, ServiceNames.CowService);
    requests.Should().Contain(r => r.RequestUri!.AbsolutePath == "/milk"
        && r.Method == HttpMethod.Get);
}
```

### Goat Service Assertions

```csharp
private async Task The_goat_service_should_have_received_a_goat_milk_request()
{
    var requests = _fakeRequestStore.GetRequests(_requestId, ServiceNames.GoatService);
    requests.Should().Contain(r => r.RequestUri!.AbsolutePath == "/goat-milk"
        && r.Method == HttpMethod.Get);
}
```

### Supplier Service Assertions

```csharp
private async Task The_supplier_service_should_have_received_an_availability_check_for_INGREDIENT(string ingredient)
{
    var requests = _fakeRequestStore.GetRequests(_requestId, ServiceNames.SupplierService);
    requests.Should().Contain(r => r.RequestUri!.AbsolutePath == $"/ingredients/{ingredient}/availability"
        && r.Method == HttpMethod.Get);
}
```

### Kitchen Service Assertions

```csharp
private async Task The_kitchen_service_should_have_received_a_preparation_request()
{
    var requests = _fakeRequestStore.GetRequests(_requestId, ServiceNames.KitchenService);
    requests.Should().Contain(r => r.RequestUri!.AbsolutePath == "/prepare"
        && r.Method == HttpMethod.Post);
}

private async Task The_kitchen_service_should_have_received_the_correct_recipe()
{
    var request = _fakeRequestStore.GetRequests(_requestId, ServiceNames.KitchenService)
        .First(r => r.RequestUri!.AbsolutePath == "/prepare");
    var body = request.Body;
    body.Should().Contain("milk");
    body.Should().Contain("eggs");
    body.Should().Contain("flour");
}
```

### No-Call Assertions

When validation fails before downstream calls are made:

```csharp
private async Task No_calls_should_have_been_made_to_the_cow_service()
{
    _fakeRequestStore.GetRequests(_requestId, ServiceNames.CowService)
        .Should().BeEmpty();
}

private async Task No_calls_should_have_been_made_to_the_kitchen_service()
{
    _fakeRequestStore.GetRequests(_requestId, ServiceNames.KitchenService)
        .Should().BeEmpty();
}
```

### When to Add Downstream Assertions

- **Happy-path**: always include to verify correct integration calls
- **Error from downstream**: assert the service was called (but returned an error)
- **Validation failure**: use `No_calls_should_have_been_made_to_{service}()`

### Typical Downstream Calls per Endpoint

| Endpoint | Downstream Services |
|---|---|
| POST /pancakes | Cow Service (milk), optionally Goat Service |
| POST /waffles | Cow Service (milk), optionally Goat Service |
| GET /milk | Cow Service |
| GET /goat-milk | Goat Service |
| POST /orders | Kitchen Service (preparation), Supplier Service (availability) |
| PATCH /orders/{id}/status | None (state machine only) |
| GET /menu | Supplier Service (availability check) |
| DELETE /menu/cache | None (in-memory cache only) |
| DELETE /toppings/{id} | None (in-memory store only) |
| GET /health | None |
| GET / | None (heartbeat) |

## Recipe Logging Assertions

When order/recipe logging is implemented, assert the logged recipe matches the actual ingredients:

> **Post-deployment mode:** Event store and Kafka message store assertions depend on in-memory infrastructure unavailable in post-deployment mode. Decorate event assertion steps with `[SkipStepIf(nameof(ComponentTestSettings.RunAgainstExternalServiceUnderTest), IgnoreReasons.EventStoreIsUnavailableInPostDeploymentEnvironments)]` and Kafka steps with `[SkipStepIf(..., IgnoreReasons.KafkaIsUnavailableInPostDeploymentEnvironments)]`. If the event/Kafka assertion is the **primary purpose** of the scenario (e.g. scenario name contains "Event Published"), use `[IgnoreIf]` at the scenario level instead. See [test-infrastructure.md](test-infrastructure.md#two-skip-mechanisms).

```csharp
private async Task<CompositeStep> The_order_recipe_should_have_been_logged_with_all_ingredients()
{
    return Sub.Steps(
        _ => The_recipe_log_should_contain_an_entry_for_the_order(),
        _ => The_recipe_log_ingredients_should_match_the_request(),
        _ => The_recipe_log_toppings_should_match_the_request());
}
```

## Response Field Assertions via Property Injection

When a response is read back from a different endpoint than the one that wrote the data (e.g. create a batch via POST, read via GET), assert field values using **property injection** with `_should_match_the_created_batch()` methods — not parameterised methods that display raw values.

### Why

Parameterised methods render as `[expectedIngredient: "Some_Fresh_Milk"]` in YAML specs, which exposes implementation details. Property-injection methods read as natural business assertions: "The retrieved batch ingredients should match the created batch".

### Pattern

```csharp
// Expected properties
public string? ExpectedMilk { get; set; }
public string? ExpectedEggs { get; set; }

public async Task The_retrieved_batch_milk_should_match_the_created_batch()
{
    _response.Should().NotBeNull();
    _response!.Ingredients.Should().Contain(ExpectedMilk);
}

// Feature steps file — helper sets all expected values from the creation response
private void SetExpectedBatchValues()
{
    _getBatchSteps.ExpectedMilk = _milkResponse.Milk;
    _getBatchSteps.ExpectedEggs = _eggsResponse.Eggs;
}

// Composite uses parameterless methods
private async Task<CompositeStep> The_retrieved_batch_should_match_the_created_batch()
{
    SetExpectedBatchValues();
    return Sub.Steps(
        _ => The_retrieved_batch_milk_should_match_the_created_batch(),
        _ => The_retrieved_batch_eggs_should_match_the_created_batch(),
        _ => The_retrieved_batch_flour_should_match_the_created_batch());
}
```

**Exception:** Keep parameterised methods when the specific value is the point of the test (e.g. `The_response_should_have_TOPPING_count(3)` in a topping limit scenario).

## Batch ID and Order ID Assertions

```csharp
private async Task The_response_should_have_a_valid_batch_id()
    => _pancakeResponse!.BatchId.Should().NotBe(Guid.Empty);

private async Task The_order_response_should_have_an_order_id()
    => _orderResponse!.OrderId.Should().NotBe(Guid.Empty);

private async Task The_order_status_should_be_STATUS(string status)
    => _orderResponse!.Status.Should().Be(status);
```

## Service Unavailable Assertions

When a downstream service is unavailable:

```csharp
private async Task<CompositeStep> The_milk_error_should_indicate_cow_service_unavailable()
{
    return Sub.Steps(
        _ => The_response_http_status_should_be_service_unavailable(),
        _ => The_error_message_should_indicate_milk_source_unavailable());
}

private async Task<CompositeStep> The_order_error_should_indicate_kitchen_busy()
{
    return Sub.Steps(
        _ => The_response_http_status_should_be_service_unavailable(),
        _ => The_error_message_should_indicate_kitchen_is_busy());
}
```

## Rate Limiting (429) Assertions

When an endpoint is rate-limited via `[EnableRateLimiting]`:

```csharp
private async Task The_second_request_should_be_rate_limited()
    => _secondResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
```

Test setup uses `delayAppCreation: true` with config overrides to set a low permit limit:

```csharp
private async Task The_rate_limit_is_configured_to_allow_one_request_per_window()
    => EnsureAppCreated(new Dictionary<string, string?>
    {
        [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.PermitLimit)}"] = "1",
        [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.WindowSeconds)}"] = "60"
    });
```

## Pagination Assertions

For paginated list endpoints returning `PaginatedResponse<T>`:

```csharp
private async Task<CompositeStep> The_paginated_response_should_contain_the_correct_metadata()
{
    return Sub.Steps(
        _ => The_list_response_should_be_ok(),
        _ => The_list_response_should_be_valid_json(),
        _ => The_response_should_contain_the_created_orders(),
        _ => The_page_number_should_be_one(),
        _ => The_total_count_should_match_the_created_order_count());
}

private async Task The_paginated_response_should_be_empty()
{
    return Sub.Steps(
        _ => The_list_response_should_be_ok(),
        _ => The_list_response_should_be_valid_json(),
        _ => The_items_list_should_be_empty(),
        _ => The_total_count_should_be_zero());
}
```

## Collection Ordering Assertions

Use AwesomeAssertions ordering methods:

```csharp
private async Task The_audit_logs_should_be_ordered_by_timestamp_descending()
    => _getAuditLogsSteps.Response!.Select(r => r.Timestamp)
        .Should().BeInDescendingOrder();

private async Task The_menu_items_should_be_in_alphabetical_order()
    => _getMenuSteps.Response!.Items.Should().BeInAscendingOrder(m => m.Name);
```

## Empty Collection Assertions

```csharp
private async Task The_audit_logs_list_should_be_empty()
    => _getAuditLogsSteps.Response.Should().BeEmpty();
```

## Cross-Field Validation Assertions

When validation depends on config-driven limits:

```csharp
private async Task The_error_message_should_reference_the_item_limit()
{
    var content = await _orderSteps.ResponseMessage!.Content.ReadAsStringAsync();
    var problemDetails = Json.Deserialize<ValidationProblemDetails>(content);
    var errors = problemDetails?.Errors.Values.SelectMany(v => v).ToList();
    errors.Should().Contain(e => e.Contains("cannot contain more than 2 items"));
}
```

## Health Check Assertions

### Degraded Status

```csharp
private async Task The_overall_status_should_be_degraded()
    => _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Degraded);

private async Task The_cow_service_dependency_should_report_degraded()
{
    _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.CowService);
    _healthCheckResult.Results[HealthCheckNames.CowService].Status
        .Should().Be(HealthCheckStatuses.Degraded);
}
```

## Structured Logging Assertions

Use `InMemoryLoggerProvider` to capture and assert on log messages:

```csharp
private async Task The_log_should_contain_an_order_created_message()
    => _logProvider.Entries.Should().Contain(e => e.Message.Contains("created for customer"));

private async Task The_log_message_should_include_the_customer_name()
    => _logProvider.Entries.Should().Contain(e =>
        e.Message.Contains(_orderSteps.Request.CustomerName!));
```
