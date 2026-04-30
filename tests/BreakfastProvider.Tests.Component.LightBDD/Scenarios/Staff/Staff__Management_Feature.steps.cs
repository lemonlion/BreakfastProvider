using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Staff;
using BreakfastProvider.Tests.Component.Shared.Models.Staff;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Staff;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Staff__Management_Feature : BaseFixture
{
    private readonly PostStaffSteps _postSteps;
    private readonly GetStaffSteps _getSteps;
    private int _createdMemberId;

    public Staff__Management_Feature()
    {
        _postSteps = Get<PostStaffSteps>();
        _getSteps = Get<GetStaffSteps>();
    }

    #region Given

    private async Task A_valid_staff_member_request()
    {
        _postSteps.Request = new TestStaffMemberRequest
        {
            Name = $"Chef-{Guid.NewGuid():N}",
            Role = "Chef",
            Email = $"chef-{Guid.NewGuid():N}@breakfast.test",
            IsActive = true,
            HiredAt = DateTime.UtcNow.AddMonths(-6)
        };
    }

    private async Task<CompositeStep> A_staff_member_exists()
    {
        return Sub.Steps(
            _ => A_valid_staff_member_request(),
            _ => The_staff_member_is_submitted(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdMemberId = _postSteps.Response!.Id;
    }

    private async Task A_staff_member_request_with_an_invalid_role()
    {
        _postSteps.Request = new TestStaffMemberRequest
        {
            Name = "Test Staff",
            Role = "InvalidRole",
            Email = $"test-{Guid.NewGuid():N}@breakfast.test"
        };
    }

    #endregion

    #region When

    private async Task The_staff_member_is_submitted()
        => await _postSteps.Send();

    private async Task The_staff_member_is_retrieved_by_id()
        => await _getSteps.RetrieveById(_createdMemberId);

    private async Task The_staff_member_is_deleted()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"staff/{_createdMemberId}");
        request.Headers.Add(Constants.CustomHeaders.ComponentTestRequestId, RequestId);
        var response = await Client.SendAsync(request);
        _deleteResponse = response;
    }

    private HttpResponseMessage? _deleteResponse;

    #endregion

    #region Then

    private async Task<CompositeStep> The_staff_response_should_contain_the_created_member()
    {
        return Sub.Steps(
            _ => The_post_response_http_status_should_be_created(),
            _ => The_post_response_should_be_valid_json(),
            _ => The_created_member_should_have_the_correct_name(),
            _ => The_created_member_should_have_the_correct_role());
    }

    private async Task The_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_post_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_created_member_should_have_the_correct_name()
        => _postSteps.Response!.Name.Should().Be(_postSteps.Request.Name);

    private async Task The_created_member_should_have_the_correct_role()
        => _postSteps.Response!.Role.Should().Be("Chef");

    private async Task<CompositeStep> The_staff_get_response_should_contain_the_member()
    {
        return Sub.Steps(
            _ => The_get_response_http_status_should_be_ok(),
            _ => The_get_response_should_be_valid_json(),
            _ => The_retrieved_member_should_match_the_created_member());
    }

    private async Task The_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_get_response_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_retrieved_member_should_match_the_created_member()
    {
        _getSteps.Response!.Id.Should().Be(_createdMemberId);
        _getSteps.Response!.Name.Should().Be(_postSteps.Response!.Name);
        _getSteps.Response!.Role.Should().Be("Chef");
    }

    private async Task The_staff_delete_response_should_indicate_no_content()
        => _deleteResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_staff_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
