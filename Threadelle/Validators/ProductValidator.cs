using FluentValidation;
using Threadelle.Models;

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.CategoryId).GreaterThan(0).WithMessage("Please select a valid category.");

        RuleFor(p => p.Name).NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(150) .WithMessage("Product name cannot exceed 150 characters.");

        RuleFor(p => p.Slug).NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(170).WithMessage("Slug cannot exceed 170 characters.");

        RuleFor(p => p.WorkHours).GreaterThanOrEqualTo(0).WithMessage("Work hours must be greater than or equal to 0.");

        RuleFor(p => p.HourRate).GreaterThanOrEqualTo(0).WithMessage("Hourly rate must be greater than or equal to 0.");

        RuleFor(p => p.Quantity).GreaterThanOrEqualTo(0).WithMessage("Quantity must be greater than or equal to 0.");

        RuleFor(p => p.StoryTelling).MaximumLength(5000).WithMessage("Story telling cannot exceed 5000 characters.");

        RuleFor(p => p.Quantity).LessThanOrEqualTo(1) .When(p => p.IsOnePiece).WithMessage("A one-piece product cannot have a quantity greater than 1.");
    }
}