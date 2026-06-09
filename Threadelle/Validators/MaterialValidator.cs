using FluentValidation;
using Threadelle.Models;

public class MaterialValidator : AbstractValidator<Material>
{
    public MaterialValidator()
    {
        RuleFor(material => material.Name).NotEmpty().WithMessage("Material name is required.")
            .MaximumLength(100).WithMessage("Material name cannot exceed 100 characters.");

        RuleFor(material => material.PricePerUnit)
            .GreaterThanOrEqualTo(0).WithMessage("Price per unit must be greater than or equal to 0.");

        RuleFor(material => material.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be greater than or equal to 0.");
    }
}