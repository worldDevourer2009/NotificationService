using FluentValidation;
using TaskHandler.Shared.Notifications.DTOs;

namespace NotificationService.Application.Common.Validators;

public class EmailRequestDTOValidator : AbstractValidator<EmailRequestDTO>
{
    public EmailRequestDTOValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject can't be empty")
            .NotNull()
            .WithMessage("Subject can't be null");
        
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty")
            .NotNull()
            .WithMessage("Message can't be null");
    }
}

public record IntNotifDto(string Message, string Title = "Internal Notification");