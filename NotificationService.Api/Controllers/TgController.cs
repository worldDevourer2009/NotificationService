using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Commands.TelegramCommandHandlers;
using TaskHandler.Shared.Notifications.DTOs.TgDTOs;

namespace NotificationService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TgController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public TgController(IMediator mediator, IHttpClientFactory factory)
    {
        _mediator = mediator;
        _httpClient = factory.CreateClient("AuthService");
        _httpClient.BaseAddress = new Uri("https://localhost:9500/");
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
    
    private string? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst("sub") ?? User.FindFirst("userId");
        
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        return null;
    }
}

public record UserDto(string Id, string FirstName, string LastName, string Email, DateTime CreatedAt);