using FluentValidation;
using NotificationService.Application.Commands.Emails;

namespace NotificationService.Application.Validators.Commands;

public class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .NotNull()
            .EmailAddress();
        
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject can't be empty")
            .NotNull()
            .WithMessage("Subject can't be empty")
            .Length(1, 150);

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty");
    }
}