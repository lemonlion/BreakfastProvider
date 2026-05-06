using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Staff;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Staff;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Staff;

public class Staff_Management_Tests : BaseFixture
{
    private readonly PostStaffSteps _postSteps;
    private readonly GetStaffSteps _getSteps;

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

    private async Task<int> CreateStaffMember()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        return _postSteps.Response!.Id;
    }

    [Fact]
    [HappyPath]
    public async Task Adding_a_new_staff_member_should_return_the_created_member()
    {
        // Given a valid staff member request
        _postSteps.Request = CreateValidRequest();

        // When the staff member is submitted
        await _postSteps.Send();

        // Then the response should contain the created member
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Name.Should().Be(_postSteps.Request.Name);
        _postSteps.Response!.Role.Should().Be("Chef");
        this.BDDfy();
    }

    [Fact]
    public async Task Retrieving_an_existing_staff_member_should_return_the_member()
    {
        // Given a staff member exists
        var createdMemberId = await CreateStaffMember();

        // When the staff member is retrieved by id
        await _getSteps.RetrieveById(createdMemberId);

        // Then the response should contain the member
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.Id.Should().Be(createdMemberId);
        _getSteps.Response!.Name.Should().Be(_postSteps.Response!.Name);
        _getSteps.Response!.Role.Should().Be("Chef");
        this.BDDfy();
    }

    [Fact]
    public async Task Deleting_a_staff_member_should_return_no_content()
    {
        // Given a staff member exists
        var createdMemberId = await CreateStaffMember();

        // When the staff member is deleted
        var request = new HttpRequestMessage(HttpMethod.Delete, $"staff/{createdMemberId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var deleteResponse = await Client.SendAsync(request);

        // Then the response should indicate no content
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        this.BDDfy();
    }

    [Fact]
    public async Task Adding_a_staff_member_with_an_invalid_role_should_return_a_bad_request_response()
    {
        // Given a staff member request with an invalid role
        _postSteps.Request = new TestStaffMemberRequest
        {
            Name = "Test Staff",
            Role = "InvalidRole",
            Email = $"test-{Guid.NewGuid():N}@breakfast.test"
        };

        // When the staff member is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        this.BDDfy();
    }
}
