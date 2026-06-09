using FluentValidation;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Validators
{
    public class MaterialUpsertViewModelValidator : AbstractValidator<MaterialUpsertViewModel>
    {
        public MaterialUpsertViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Material name is required.")
                .MaximumLength(100).WithMessage("Material name cannot exceed 100 characters.");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Unit is required.")
                .MaximumLength(30).WithMessage("Unit cannot exceed 30 characters.");

            RuleFor(x => x.PricePerUnit)
                .GreaterThanOrEqualTo(0).WithMessage("Price per unit must be 0 or greater.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be 0 or greater.");
        }
    }
}
