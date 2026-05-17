using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Staff;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Staff;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Staff;

public class Staff_Management_Tests : BaseFixture
{
    private readonly PostStaffSteps _postSteps;
    private readonly GetStaffSteps _getSteps;

    private int _createdMemberId;
    private HttpResponseMessage? _deleteResponse;

    public Staff_Management_Tests()
    {
        _postSteps = Get<PostStaffSteps>();
        _getSteps = Get<GetStaffSteps>();
    }

    private TestStaffMemberRequest CreateValidRequest() => new()
    {
        Name = $"Chef-{Guid.NewGuid():N}",
        Role = "Chef",
        Email = $"chef-{Guid.NewGuid():N}@breakfast.test",
        IsActive = true,
        HiredAt = DateTime.UtcNow.AddMonths(-6)
    };

    [Fact]
    [HappyPath]
    public void Adding_a_new_staff_member_should_return_the_created_member()
    {
        this.Given(x => x.A_valid_staff_member_request_is_prepared())
            .When(x => x.The_staff_member_is_submitted())
            .Then(x => x.The_response_should_contain_the_created_member())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_an_existing_staff_member_should_return_the_member()
    {
        this.Given(x => x.A_staff_member_exists())
            .When(x => x.The_staff_member_is_retrieved_by_id())
            .Then(x => x.The_get_response_should_contain_the_member())
            .BDDfy();
    }

    [Fact]
    public void Deleting_a_staff_member_should_return_no_content()
    {
        this.Given(x => x.A_staff_member_exists())
            .When(x => x.The_staff_member_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_no_content())
            .BDDfy();
    }

    [Fact]
    public void Adding_a_staff_member_with_an_invalid_role_should_return_a_bad_request_response()
    {
        this.Given(x => x.A_staff_member_request_with_an_invalid_role_is_prepared())
            .When(x => x.The_staff_member_is_submitted())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_staff_member_request_is_prepared()
    {
        _postSteps.Request = CreateValidRequest();
        await Task.CompletedTask;
    }

    private async Task The_staff_member_is_submitted()
    {
        await _postSteps.Send();
    }

    private async Task The_response_should_contain_the_created_member()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Name.Should().Be(_postSteps.Request!.Name);
        _postSteps.Response!.Role.Should().Be("Chef");
    }

    private async Task A_staff_member_exists()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdMemberId = _postSteps.Response!.Id;
    }

    private async Task The_staff_member_is_retrieved_by_id()
    {
        await _getSteps.RetrieveById(_createdMemberId);
    }

    private async Task The_get_response_should_contain_the_member()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.Id.Should().Be(_createdMemberId);
        _getSteps.Response!.Name.Should().Be(_postSteps.Response!.Name);
        _getSteps.Response!.Role.Should().Be("Chef");
    }

    private async Task The_staff_member_is_deleted()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"staff/{_createdMemberId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _deleteResponse = await Client.SendAsync(request);
    }

    private void The_delete_response_should_indicate_no_content()
    {
        _deleteResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task A_staff_member_request_with_an_invalid_role_is_prepared()
    {
        _postSteps.Request = new TestStaffMemberRequest
        {
            Name = "Test Staff",
            Role = "InvalidRole",
            Email = $"test-{Guid.NewGuid():N}@breakfast.test"
        };
        await Task.CompletedTask;
    }

    private void The_post_response_should_indicate_bad_request()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
