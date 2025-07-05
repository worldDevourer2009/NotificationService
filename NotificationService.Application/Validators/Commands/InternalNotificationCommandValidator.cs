using FluentValidation;
using NotificationService.Application.Commands.InternalNotificationsCommandHandlers;

namespace NotificationService.Application.Validators.Commands;

public class InternalNotificationCommandValidator : AbstractValidator<InternalNotificationCommand>
{
    public InternalNotificationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .Must(x => Guid.TryParse(x, out _))
            .WithMessage("Id must be a valid Guid");
        
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title can't be empty");
        
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty");
    }
}