using BreakfastProvider.Api.Models.Requests;
using FluentValidation;

namespace BreakfastProvider.Api.Validators;

public class RecipeReviewRequestValidator : AbstractValidator<RecipeReviewRequest>
{
    public RecipeReviewRequestValidator()
    {
        RuleFor(x => x.RecipeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReviewerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comments).MaximumLength(1000);
        RuleFor(x => x.Tags).Must(t => t == null || t.Count <= 10)
            .WithMessage("A maximum of 10 tags is allowed.");
    }
}
