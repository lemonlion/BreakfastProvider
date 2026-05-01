using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public class MilkSourcingService(
    IHttpClientFactory httpClientFactory,
    ILogger<MilkSourcingService> logger) : IMilkSourcingService
{
    public async Task<MilkResponse> SourceFromCowAsync(CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MilkSourcingService.SourceFromCow");

        var responseMessage = await FetchFromDownstreamAsync(HttpClientNames.CowService, "milk", cancellationToken);
        var milkResponse = await DeserializeResponseAsync<MilkResponse>(responseMessage, "Cow Service", cancellationToken);
        ValidateResponse(milkResponse, "Cow Service");

        return milkResponse!;
    }

    public async Task<GoatMilkResponse> SourceFromGoatAsync(CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MilkSourcingService.SourceFromGoat");

        var responseMessage = await FetchFromDownstreamAsync(HttpClientNames.GoatService, "goat-milk", cancellationToken);
        var goatMilkResponse = await DeserializeResponseAsync<GoatMilkResponse>(responseMessage, "Goat Service", cancellationToken);
        ValidateResponse(goatMilkResponse, "Goat Service");

        return goatMilkResponse!;
    }

    private async Task<HttpResponseMessage> FetchFromDownstreamAsync(string clientName, string path, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MilkSourcingService.FetchFromDownstream");
        activity?.SetTag("downstream.client", clientName);
        activity?.SetTag("downstream.path", path);

        var client = httpClientFactory.CreateClient(clientName);
        var response = await client.GetAsync(path, cancellationToken);

        activity?.SetTag("downstream.status_code", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("{ClientName} returned status {StatusCode}", clientName, response.StatusCode);
            throw new HttpRequestException($"The {clientName} returned an error.");
        }

        return response;
    }

    private async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage responseMessage, string serviceName, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MilkSourcingService.DeserializeResponse");
        activity?.SetTag("downstream.service", serviceName);

        var result = await responseMessage.Content.ReadFromJsonAsync<T>(cancellationToken);

        activity?.SetTag("downstream.response_valid", result is not null);
        return result;
    }

    private void ValidateResponse<T>(T? response, string serviceName)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("MilkSourcingService.ValidateResponse");
        activity?.SetTag("downstream.service", serviceName);
        activity?.SetTag("validation.passed", response is not null);

        if (response is null)
        {
            logger.LogWarning("{ServiceName} returned an invalid response", serviceName);
            throw new HttpRequestException($"The {serviceName} returned an invalid response.");
        }
    }
}
