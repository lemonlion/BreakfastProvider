using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework.Parameters;
using Microsoft.AspNetCore.Mvc;
using TestTrackingDiagrams.LightBDD;
using static System.Text.RegularExpressions.Regex;

namespace BreakfastProvider.Tests.Component.LightBDD.Common.Validation;

public static class ValidationHelper
{
    public static List<T> CreateValidationRequests<T>(T validBase, InputTable<InvalidFieldFromRequest> inputs)
        where T : class
    {
        var requests = new List<T>();

        foreach (var invalidField in inputs)
            requests.Add(validBase.GetWithPropertyValueChanged(invalidField.Field!, invalidField.Value));

        return requests;
    }

    public static async Task<List<HttpResponseMessage>> SendValidationRequests<T>(
        HttpClient client, string requestId, string endpoint, List<T> requests,
        IReadOnlyList<InvalidFieldFromRequest>? invalidFields = null)
    {
        return await SendValidationRequests(HttpMethod.Post, client, requestId, endpoint, requests, invalidFields);
    }

    public static async Task<List<HttpResponseMessage>> SendPatchValidationRequests<T>(
        HttpClient client, string requestId, string endpoint, List<T> requests,
        IReadOnlyList<InvalidFieldFromRequest>? invalidFields = null)
    {
        return await SendValidationRequests(HttpMethod.Patch, client, requestId, endpoint, requests, invalidFields);
    }

    public static async Task<List<HttpResponseMessage>> SendPutValidationRequests<T>(
        HttpClient client, string requestId, string endpoint, List<T> requests,
        IReadOnlyList<InvalidFieldFromRequest>? invalidFields = null)
    {
        return await SendValidationRequests(HttpMethod.Put, client, requestId, endpoint, requests, invalidFields);
    }

    private static async Task<List<HttpResponseMessage>> SendValidationRequests<T>(
        HttpMethod method, HttpClient client, string requestId, string endpoint, List<T> requests,
        IReadOnlyList<InvalidFieldFromRequest>? invalidFields = null)
    {
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < requests.Count; i++)
        {
            if (invalidFields is not null)
                TrackingDiagramOverride.InsertTestDelimiter(
                    $"The Field '{invalidFields[i].Field}' Set To {invalidFields[i].Value}");

            var request = new HttpRequestMessage(method, endpoint)
            {
                Content = System.Net.Http.Json.JsonContent.Create(requests[i])
            };
            request.Headers.Add(CustomHeaders.ComponentTestRequestId, requestId);
            responses.Add(await client.SendAsync(request));
        }
        return responses;
    }

    public static async Task<List<VerifiableErrorResult>> ParseValidationResponses(
        List<HttpResponseMessage> responses)
    {
        var actualResults = new List<VerifiableErrorResult>();
        foreach (var response in responses)
        {
            string? errorMessage = null;
            var content = await response.Content.ReadAsStringAsync();
            var problemDetails = Json.Deserialize<ValidationProblemDetails>(content);
            var firstError = problemDetails?.Errors.FirstOrDefault();
            if (firstError is { Value.Length: > 0 })
                errorMessage = firstError.Value.Value.First();

            actualResults.Add(new VerifiableErrorResult(errorMessage, Titleize(response.StatusCode.ToString())));
        }
        return actualResults;
    }

    public static string Titleize(string input) =>
        Replace(input, "(?<=[a-z])([A-Z])", " $1");
}
