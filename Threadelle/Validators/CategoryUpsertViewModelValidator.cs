using FluentValidation;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Validators
{
    public class CategoryUpsertViewModelValidator : AbstractValidator<CategoryUpsertViewModel>
    {
        public CategoryUpsertViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be 0 or greater.");

            RuleFor(x => x.ImageFile)
                .Must(f => f == null || f.Length <= 5 * 1024 * 1024)
                .WithMessage("Image must not exceed 5 MB.")
                .Must(f => f == null || new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }
                    .Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Only JPG, PNG, WEBP or GIF images are accepted.");
        }
    }
}
