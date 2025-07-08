using FluentValidation;
using NotificationService.Application.Commands.TelegramCommandHandlers;

namespace NotificationService.Application.Validators.Commands;

public class NotifyTelegramCommandValidator : AbstractValidator<NotifyTelegramCommand>
{
    public NotifyTelegramCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty");
    }
}