using FluentValidation;
using Threadelle.Models;

public class NotificationValidator : AbstractValidator<Notification>
{
    public NotificationValidator()
    {

        RuleFor(notification => notification.Title)
            .NotEmpty().WithMessage("Notification title is required.")
            .MaximumLength(200).WithMessage("Notification title cannot exceed 200 characters.");

        RuleFor(notification => notification.Message).NotEmpty().WithMessage("Notification message is required.");

        RuleFor(notification => notification.RelatedUrl).MaximumLength(300).WithMessage("Related URL cannot exceed 300 characters.");
    }
}