using FluentValidation;
using TaskHandler.Shared.Notifications.DTOs.TgDTOs;

namespace NotificationService.Application.Common.Validators;

public class NotifyTelegramDtoValidator : AbstractValidator<NotifyTelegramDto>
{
    public NotifyTelegramDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty")
            .NotNull()
            .WithMessage("Message can't be null");
    }
}