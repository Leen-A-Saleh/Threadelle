using FluentValidation;
using Threadelle.Models;

namespace Threadelle.Validators
{
    public class CategoryValidator : AbstractValidator<Category>
    {
        public CategoryValidator()
        {
            RuleFor(cat => cat.Name).NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

            RuleFor(cat => cat.Slug).NotEmpty().WithMessage("Slug is required.")
                .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.");

            RuleFor(cat => cat.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0.");
        }
    }
}
