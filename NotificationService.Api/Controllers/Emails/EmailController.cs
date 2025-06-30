using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskHandler.Application.Commands.Emails;
using TaskHandler.Application.DTOs;

namespace NotificationService.Api.Controllers.Emails;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequestDTO emailRequestDto)
    {
        var command = new SendEmailCommand(
            emailRequestDto.Email, 
            emailRequestDto.Subject,
            emailRequestDto.Message,
            emailRequestDto.HtmlMessage,
            null);
        
        var result = await _mediator.Send(command);

        if (result == null)
        {
            return BadRequest("Can't send email");
        }

        return Ok("Email sent successfully");
    }
}