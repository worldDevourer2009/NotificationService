using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Commands.TelegramCommandHandlers;
using NotificationService.Shared.DTOs.TgDTOs;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TgController : ControllerBase
{
    private readonly IMediator _mediator;

    public TgController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send-notification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification([FromBody] NotifyTelegramDto request)
    {
        var command = new NotifyTelegramCommand(request.Message);
        
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response.Message);
        }
        
        return Ok(response.Message);
    }
}