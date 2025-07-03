using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.TelegramCommandHandlers;

public record NotifyTelegramCommand(string? Message) : ICommand<NotifyTelegramCommandResponse>;
public record NotifyTelegramCommandResponse(bool Success, string? Message);

public class NotifyTelegramCommandHandler : ICommandHandler<NotifyTelegramCommand, NotifyTelegramCommandResponse>
{
    private readonly ITelegramService _telegramService;

    public NotifyTelegramCommandHandler(ITelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task<NotifyTelegramCommandResponse> Handle(NotifyTelegramCommand request, CancellationToken cancellationToken)
    {
        if (request.Message is null)
        {
            return new NotifyTelegramCommandResponse(false, "Message is null");
        }
        
        await _telegramService.SendTelegramMessageAsync("1", request.Message, cancellationToken);
        
        return new NotifyTelegramCommandResponse(true, "Message sent");
    }
}