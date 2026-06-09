using FluentValidation;
using Threadelle.Models;

public class CustomOrderValidator : AbstractValidator<CustomOrder>
{
    public CustomOrderValidator()
    {

        RuleFor(customOrder => customOrder.Title).MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(customOrder => customOrder.Budget)
            .GreaterThan(0).When(customOrder => customOrder.Budget.HasValue)
            .WithMessage("Budget must be greater than 0.");

        RuleFor(customOrder => customOrder.EstimatedPrice)
            .GreaterThan(0).When(customOrder => customOrder.EstimatedPrice.HasValue)
            .WithMessage("Estimated price must be greater than 0.");

        RuleFor(customOrder => customOrder.EstimatedDays)
            .GreaterThan(0).When(customOrder => customOrder.EstimatedDays.HasValue)
            .WithMessage("Estimated days must be greater than 0.");
    }
}