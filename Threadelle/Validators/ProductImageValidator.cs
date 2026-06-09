using FluentValidation;
using Threadelle.Models; 

public class ProductImageValidator : AbstractValidator<ProductImage>
{
    public ProductImageValidator()
    {

        RuleFor(PImg => PImg.ImageUrl).NotEmpty().WithMessage("Image URL is required.");

        RuleFor(PImg => PImg.DisplayOrder) .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0.");
    }
}