using BreakfastProvider.Api.Models.Events;

namespace BreakfastProvider.Api.Services;

public interface IRecipeLogger
{
    Task LogRecipeAsync(RecipeLogEvent recipe, CancellationToken cancellationToken = default);
}
