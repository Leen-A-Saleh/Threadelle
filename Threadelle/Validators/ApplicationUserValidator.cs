using FluentValidation;
using Threadelle.Models;

namespace Threadelle.Validators
{
    public class ApplicationUserValidator : AbstractValidator<ApplicationUser>
    {
        public ApplicationUserValidator()
        {
            RuleFor(user => user.FullName).NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(80).WithMessage("Full Name cannot exceed 80 characters.");
            RuleFor(user=>user.BirthDate).LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Birth Date must be in the past.");
        }
    }
}