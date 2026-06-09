using FluentValidation;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Validators
{
    public class ProductUpsertViewModelValidator : AbstractValidator<ProductUpsertViewModel>
    {
        public ProductUpsertViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(150).WithMessage("Product name cannot exceed 150 characters.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Please select a category.");

            RuleFor(x => x.WorkHours)
                .GreaterThanOrEqualTo(0).WithMessage("Work hours must be 0 or greater.");

            RuleFor(x => x.HourRate)
                .GreaterThanOrEqualTo(0).WithMessage("Hour rate must be 0 or greater.");

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Quantity must be 0 or greater.")
                .LessThanOrEqualTo(1).When(x => x.IsOnePiece)
                .WithMessage("A one-piece product cannot have a quantity greater than 1.");

            RuleFor(x => x.Description)
                .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

            RuleFor(x => x.StoryTelling)
                .MaximumLength(5000).WithMessage("Story cannot exceed 5000 characters.");

            RuleForEach(x => x.NewImages)
                .Must(f => f.Length <= 8 * 1024 * 1024)
                .WithMessage("Each image must not exceed 8 MB.")
                .Must(f => new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }
                    .Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Only JPG, PNG, WEBP or GIF images are accepted.");
        }
    }
}
