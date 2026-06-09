using FluentValidation;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Validators
{
    public class ColorUpsertViewModelValidator : AbstractValidator<ColorUpsertViewModel>
    {
        public ColorUpsertViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Color name is required.")
                .MaximumLength(50).WithMessage("Color name cannot exceed 50 characters.");

            RuleFor(x => x.HexCode)
                .NotEmpty().WithMessage("Hex code is required.")
                .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Hex code must be a valid 6-digit color (e.g. #A86F6F).");
        }
    }
}
