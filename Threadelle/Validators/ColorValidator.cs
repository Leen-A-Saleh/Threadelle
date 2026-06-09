using FluentValidation;
using Threadelle.Models;

public class ColorValidator : AbstractValidator<Color>
{
    public ColorValidator()
    {
        RuleFor(color => color.Name).NotEmpty().WithMessage("Color name is required.")
            .MaximumLength(50).WithMessage("Color name cannot exceed 50 characters.");

        RuleFor(color => color.HexCode).Matches("^#([A-Fa-f0-9]{6})$").When(color => !string.IsNullOrWhiteSpace(color.HexCode))
            .WithMessage("Hex code must be a valid color format (e.g. #FF5733).");
    }
}