using Darkhorse.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Darkhorse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var userId = await mediator.Send(command);
        return Ok(new { userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        Response.Cookies.Append("csrf_token", Guid.NewGuid().ToString(), new CookieOptions
        {
            HttpOnly = false, // Must be readable by frontend JS to echo back
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("csrf_token");
        var jti = User.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
        if (!string.IsNullOrEmpty(jti))
        {
            await mediator.Send(new LogoutCommand(jti));
        }
        return Ok();
    }
}
