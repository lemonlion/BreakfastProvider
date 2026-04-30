using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AsyncApi;

#pragma warning disable CS1998
public partial class AsyncApi__Specification_Feature : BaseFixture
{
    private HttpResponseMessage? _asyncApiResponse;
    private string? _asyncApiJsonString;
    private string? _asyncApiJsonStringToPublish;
    private JsonDocument? _asyncApiJson;
    
    #region Given
    #endregion

    #region When

    private async Task The_asyncapi_endpoint_is_called()
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _asyncApiResponse = await Client.GetAsync(Endpoints.AsyncApi.AsyncApiSpec);
                return;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                await Task.Delay(200 * attempt);
            }
        }
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_valid()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_valid_json());
    }

    private async Task The_response_status_should_be_ok()
    {
        _asyncApiResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task The_response_should_be_valid_json()
    {
        _asyncApiJsonString = await _asyncApiResponse!.Content.ReadAsStringAsync();
        Json.TryParse(_asyncApiJsonString, out _asyncApiJson).Should().BeTrue();
    }

    private async Task<CompositeStep> The_asyncapi_spec_is_written_to_disk()
    {
        return Sub.Steps(
            _ => The_asyncapi_spec_should_contain_the_dev_portal_publisher_section(),
            _ => The_asyncapi_spec_should_contain_NAME("asyncapi"),
            _ => The_asyncapi_spec_should_contain_NAME("info"),
            _ => The_asyncapi_spec_should_contain_NAME("defaultContentType"),
            _ => The_asyncapi_spec_should_contain_NAME("channels"),
            _ => The_asyncapi_spec_should_contain_NAME("operations"),
            _ => The_asyncapi_spec_should_contain_NAME("components"),
            _ => The_asyncapi_spec_is_written_to_disk_as_json());
    }

    private async Task The_asyncapi_spec_is_modified_to_have_x_pub_section()
    {
        var openApiDocument = (JsonObject)JsonNode.Parse(_asyncApiJsonString!)!;
        var serializationOptions = new JsonSerializerOptions(Json.SerializerOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        openApiDocument.Add("x-pub-settings", new JsonObject
        {
            { "pub-ready", true },
            { "tags", new JsonArray { "Breakfast" } },
            { "team", "Griddle" }
        });

        _asyncApiJsonStringToPublish = JsonSerializer.Serialize(openApiDocument, serializationOptions);
        _asyncApiJson = JsonDocument.Parse(_asyncApiJsonStringToPublish);
    }

    private async Task<CompositeStep> The_asyncapi_spec_should_contain_the_dev_portal_publisher_section()
    {
        return Sub.Steps(
            _ => The_asyncapi_spec_is_modified_to_have_x_pub_section(),
            _ => The_asyncapi_spec_should_contain_an_x_pub_settings_section(),
            _ => The_x_pub_settings_section_should_contain_pub_ready_as_true(),
            _ => The_x_pub_settings_section_should_contain_team_set_to_Griddle(),
            _ => The_x_pub_settings_section_should_contain_a_collection_of_tags_with_only_Breakfast_in_it());
    }

    private async Task The_asyncapi_spec_should_contain_an_x_pub_settings_section()
    {
        _asyncApiJson!.RootElement.GetProperty("x-pub-settings").Should().NotBeNull();
    }

    private async Task The_x_pub_settings_section_should_contain_pub_ready_as_true()
    {
        var pubReady = _asyncApiJson!.RootElement.GetProperty("x-pub-settings").GetProperty("pub-ready");
        pubReady.Should().NotBeNull();
        pubReady.GetBoolean().Should().BeTrue();
    }

    private async Task The_x_pub_settings_section_should_contain_team_set_to_Griddle()
    {
        var team = _asyncApiJson!.RootElement.GetProperty("x-pub-settings").GetProperty("team");
        team.Should().NotBeNull();
        team.GetString().Should().Be("Griddle");
    }

    private async Task The_x_pub_settings_section_should_contain_a_collection_of_tags_with_only_Breakfast_in_it()
    {
        var tags = _asyncApiJson!.RootElement.GetProperty("x-pub-settings").GetProperty("tags");
        tags.Should().NotBeNull();
        tags.EnumerateArray().Should().OnlyContain(x => x.GetString() == "Breakfast");
    }

    private async Task The_asyncapi_spec_should_contain_NAME(string name)
        => _asyncApiJson!.RootElement.GetProperty(name).Should().NotBeNull();

    private async Task The_asyncapi_spec_is_written_to_disk_as_json()
    {
        var path = $"{AsyncApiSpecs.SpecificationsFolderPath}{AsyncApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, _asyncApiJsonStringToPublish, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }

    #endregion
}
