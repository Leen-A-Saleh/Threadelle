using FluentValidation;
using Threadelle.Models;

public class TestimonialValidator : AbstractValidator<Testimonial>
{
    public TestimonialValidator()
    {
        RuleFor(testimonial => testimonial.UserId).NotEmpty().WithMessage("User is required.");
        RuleFor(testimonial => testimonial.Message) .NotEmpty().WithMessage("Testimonial message is required.");

        RuleFor(testimonial => testimonial.Rating).InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Rating must be between 1 and 5.");
    }
}