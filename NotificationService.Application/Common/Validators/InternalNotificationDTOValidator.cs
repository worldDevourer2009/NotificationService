using FluentValidation;

namespace NotificationService.Application.Common.Validators;

public class InternalNotificationDTOValidator : AbstractValidator<IntNotifDto>
{
    public InternalNotificationDTOValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty")
            .NotNull()
            .WithMessage("Message can't be null");
    }
}