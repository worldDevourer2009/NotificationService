using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Commands.Emails;
using TaskHandler.Shared.Notifications.DTOs;

namespace NotificationService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public EmailController(IMediator mediator, HttpClient httpClient)
    {
        _mediator = mediator;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://authservice-api/");
    }

    [HttpPost("send-notification-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotificationByEmail([FromBody] EmailRequestDTO dto)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }
        
        var user = await GetUser(userId);

        if (user is null)
        {
            return Unauthorized();
        }
        
        var command = new SendEmailCommand(user.Email, dto.Subject, dto.Message, dto.HtmlMessage, null);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest("Can't send email");
        }
        
        return Ok("Email sent successfully");
    }
    
    private async Task<UserDto?> GetUser(string id)
    {
        var response = await _httpClient.GetAsync($"api/user/{id}");
        
        if (response.IsSuccessStatusCode)
        {
            var userJson = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (user is null)
            {
                return null;
            }
            
            return new UserDto(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt);
        }
        
        return null;
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