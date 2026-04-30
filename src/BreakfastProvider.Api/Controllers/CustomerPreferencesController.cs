using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("customer-preferences")]
[Produces("application/json")]
[Consumes("application/json")]
public class CustomerPreferencesController(ICustomerPreferenceService preferenceService) : ControllerBase
{
    [HttpPut("{customerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerPreferenceResponse>> Upsert(string customerId, [FromBody] CustomerPreferenceRequest request, CancellationToken cancellationToken)
    {
        var response = await preferenceService.UpsertAsync(request with { CustomerId = customerId }, cancellationToken);
        return response;
    }

    [HttpGet("{customerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerPreferenceResponse>> GetById(string customerId, CancellationToken cancellationToken)
    {
        var response = await preferenceService.GetByIdAsync(customerId, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }
}
