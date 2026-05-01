using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MenuItemResponse>>> GetMenu(CancellationToken cancellationToken)
    {
        var (items, _) = await menuService.GetMenuAsync(cancellationToken);
        return items;
    }

    [HttpDelete("cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearCache()
    {
        menuService.ClearCache();
        return NoContent();
    }
}
