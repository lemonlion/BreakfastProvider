using System.Net.Http.Json;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reporting;

namespace BreakfastProvider.Tests.Component.Shared.Common.Reporting;

public class GraphQlReportingSteps(RequestContext context)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HttpResponseMessage? ResponseMessage { get; private set; }

    public List<TestOrderSummaryResponse>? OrderSummaries { get; private set; }
    public List<TestRecipeReportResponse>? RecipeReports { get; private set; }
    public List<TestIngredientUsageResponse>? IngredientUsage { get; private set; }
    public List<TestPopularRecipesResponse>? PopularRecipes { get; private set; }
    public List<TestBatchCompletionResponse>? BatchCompletions { get; private set; }
    public List<TestIngredientShipmentResponse>? IngredientShipments { get; private set; }
    public List<TestEquipmentAlertResponse>? EquipmentAlerts { get; private set; }

    public async Task QueryOrderSummaries()
    {
        var query = new
        {
            query = "{ orderSummaries { orderId customerName itemCount tableNumber status createdAt } }"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task QueryRecipeReports(int maxAttempts = 30, int delayMs = 2000, Guid? waitForOrderId = null)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var query = new
            {
                query = "{ recipeReports { orderId recipeType ingredients toppings loggedAt } }"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
            {
                Content = JsonContent.Create(query)
            };
            request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
            ResponseMessage = await context.Client.SendAsync(request);

            if (attempt >= maxAttempts || !ResponseMessage.IsSuccessStatusCode)
                break;

            var content = await ResponseMessage.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("recipeReports", out var reports) &&
                reports.GetArrayLength() > 0)
            {
                // When waiting for a specific order, keep polling until it appears
                if (waitForOrderId == null)
                    break;

                var targetId = waitForOrderId.Value.ToString();
                var found = false;
                foreach (var report in reports.EnumerateArray())
                {
                    if (report.TryGetProperty("orderId", out var oid) &&
                        oid.GetString()?.Equals(targetId, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            await Task.Delay(delayMs);
        }
    }

    public async Task QueryIngredientUsage()
    {
        var query = new
        {
            query = "{ ingredientUsage { ingredient count } }"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task QueryPopularRecipes()
    {
        var query = new
        {
            query = "{ popularRecipes { recipeType count } }"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseOrderSummariesResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("orderSummaries");
        OrderSummaries = JsonSerializer.Deserialize<List<TestOrderSummaryResponse>>(data.GetRawText(), JsonOptions)!;
    }

    public async Task ParseRecipeReportsResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("recipeReports");
        RecipeReports = JsonSerializer.Deserialize<List<TestRecipeReportResponse>>(data.GetRawText(), JsonOptions)!;
    }

    public async Task ParseIngredientUsageResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("ingredientUsage");
        IngredientUsage = JsonSerializer.Deserialize<List<TestIngredientUsageResponse>>(data.GetRawText(), JsonOptions)!;
    }

    public async Task ParsePopularRecipesResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("popularRecipes");
        PopularRecipes = JsonSerializer.Deserialize<List<TestPopularRecipesResponse>>(data.GetRawText(), JsonOptions)!;
    }

    public async Task QueryBatchCompletions()
    {
        var query = new
        {
            query = "{ batchCompletions { batchId recipeType ingredients completedAt } }"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseBatchCompletionsResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("batchCompletions");
        BatchCompletions = JsonSerializer.Deserialize<List<TestBatchCompletionResponse>>(data.GetRawText(), JsonOptions)!;
    }

    public async Task QueryIngredientShipments()
    {
        var query = new
        {
            query = "{ ingredientShipments { deliveryId ingredientName quantity deliveredAt } }"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseIngredientShipmentsResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("ingredientShipments");
        IngredientShipments = JsonSerializer.Deserialize<List<TestIngredientShipmentResponse>>(data.GetRawText(), JsonOptions)!;
    }

    public async Task QueryEquipmentAlerts()
    {
        var query = new
        {
            query = "{ equipmentAlerts { id alertId batchId equipmentName alertType alertedAt } }"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.GraphQL)
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseEquipmentAlertsResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        AssertDataIsNotNull(doc, content);
        var data = doc.RootElement.GetProperty("data").GetProperty("equipmentAlerts");
        EquipmentAlerts = JsonSerializer.Deserialize<List<TestEquipmentAlertResponse>>(data.GetRawText(), JsonOptions)!;
    }

    private static void AssertDataIsNotNull(JsonDocument doc, string rawContent)
    {
        var dataElement = doc.RootElement.GetProperty("data");
        if (dataElement.ValueKind == JsonValueKind.Null)
        {
            var errors = doc.RootElement.TryGetProperty("errors", out var errElement)
                ? errElement.GetRawText()
                : "(no errors property)";
            throw new InvalidOperationException(
                $"GraphQL response returned data:null. Errors: {errors}. Full response: {rawContent}");
        }
    }
}
