using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Models.Requests;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Validators;

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator(IOptions<OrderConfig> orderConfig)
    {
        var maxItems = orderConfig.Value.MaxItemsPerOrder;

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("'Customer Name' is required.");

        RuleFor(x => x.CustomerName)
            .MustNotContainHtmlOrScript("Customer Name")
            .When(x => x.CustomerName is not null);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required.");

        RuleFor(x => x.Items)
            .Must(items => items.Count <= maxItems)
            .WithMessage($"An order cannot contain more than {maxItems} items.")
            .When(x => x.Items is { Count: > 0 });

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ItemType)
                .NotEmpty()
                .WithMessage("'Item Type' is required.");

            item.RuleFor(x => x.ItemType)
                .MustNotContainHtmlOrScript("Item Type")
                .When(x => x.ItemType is not null);

            item.RuleFor(x => x.BatchId)
                .NotEmpty()
                .WithMessage("'Batch Id' is required.");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
        });
    }
}
