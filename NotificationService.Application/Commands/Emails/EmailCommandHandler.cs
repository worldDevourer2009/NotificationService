using NotificationService.Application.Commands;
using TaskHandler.Domain.Services;

namespace TaskHandler.Application.Commands.Emails;

public record SendEmailCommand(
    string? Email,
    string? Subject,
    string? Message,
    string? HtmlMessage,
    string[]? Attachments) : ICommand<SendEmailCommandResponse>;

public record SendEmailCommandResponse(bool Success);

public class EmailCommandHandler : ICommandHandler<SendEmailCommand, SendEmailCommandResponse>
{
    private readonly IEmailSender _emailSender;

    public EmailCommandHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task<SendEmailCommandResponse> Handle(SendEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return new SendEmailCommandResponse(false);
        }
        
        var success = await _emailSender.SendEmailAsync(
            request.Email,
            request.Subject,
            request.Message,
            request.HtmlMessage,
            request.Attachments);

        return new SendEmailCommandResponse(success);
    }
}