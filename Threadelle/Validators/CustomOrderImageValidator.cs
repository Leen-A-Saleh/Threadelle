using FluentValidation;
using Threadelle.Models;

public class CustomOrderImageValidator : AbstractValidator<CustomOrderImage>
{
    public CustomOrderImageValidator()
    {

        RuleFor(customOrderImage => customOrderImage.ImageUrl)
            .NotEmpty().WithMessage("Image URL is required.");

        RuleFor(customOrderImage => customOrderImage.Caption)
            .MaximumLength(200) .WithMessage("Caption cannot exceed 200 characters.");
    }
}