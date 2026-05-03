using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Staff;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Staff;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Staff;

[Binding]
public class StaffManagementSteps(
    AppManager appManager,
    PostStaffSteps postSteps,
    GetStaffSteps getSteps)
{
    private int _createdMemberId;
    private HttpResponseMessage? _deleteResponse;

    [Given("a valid staff member request")]
    public void GivenAValidStaffMemberRequest()
    {
        postSteps.Request = new TestStaffMemberRequest
        {
            Name = $"Chef-{Guid.NewGuid():N}",
            Role = "Chef",
            Email = $"chef-{Guid.NewGuid():N}@breakfast.test",
            IsActive = true,
            HiredAt = DateTime.UtcNow.AddMonths(-6)
        };
    }

    [Given("a staff member exists")]
    public async Task GivenAStaffMemberExists()
    {
        GivenAValidStaffMemberRequest();
        await postSteps.Send();
        Track.That(() => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await postSteps.ParseResponse();
        _createdMemberId = postSteps.Response!.Id;
    }

    [Given("a staff member request with an invalid role")]
    public void GivenAStaffMemberRequestWithAnInvalidRole()
    {
        postSteps.Request = new TestStaffMemberRequest
        {
            Name = "Test Staff",
            Role = "InvalidRole",
            Email = $"test-{Guid.NewGuid():N}@breakfast.test"
        };
    }

    [When("the staff member is submitted")]
    public async Task WhenTheStaffMemberIsSubmitted() => await postSteps.Send();

    [When("the staff member is retrieved by id")]
    public async Task WhenTheStaffMemberIsRetrievedById() => await getSteps.RetrieveById(_createdMemberId);

    [When("the staff member is deleted")]
    public async Task WhenTheStaffMemberIsDeleted()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"staff/{_createdMemberId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _deleteResponse = await appManager.Client.SendAsync(request);
    }

    [Then("the staff response should contain the created member")]
    public async Task ThenTheStaffResponseShouldContainTheCreatedMember()
    {
        Track.That(() => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await postSteps.ParseResponse();
        Track.That(() => postSteps.Response!.Name.Should().Be(postSteps.Request.Name));
        Track.That(() => postSteps.Response!.Role.Should().Be("Chef"));
    }

    [Then("the staff get response should contain the member")]
    public async Task ThenTheStaffGetResponseShouldContainTheMember()
    {
        Track.That(() => getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await getSteps.ParseResponse();
        Track.That(() => getSteps.Response!.Id.Should().Be(_createdMemberId));
        Track.That(() => getSteps.Response!.Name.Should().Be(postSteps.Response!.Name));
        Track.That(() => getSteps.Response!.Role.Should().Be("Chef"));
    }

    [Then("the staff delete response should indicate no content")]
    public void ThenTheStaffDeleteResponseShouldIndicateNoContent()
        => Track.That(() => _deleteResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent));

    [Then("the staff response should indicate bad request")]
    public void ThenTheStaffResponseShouldIndicateBadRequest()
        => Track.That(() => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
}
