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
        try
        {
            var userId = await mediator.Send(command);
            return Ok(new { userId });
        }
        catch (System.Exception ex)
        {
            return Conflict(new { detail = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        try
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
        catch (System.Exception ex)
        {
            return Unauthorized(new { detail = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await mediator.Send(new RefreshTokenCommand(request.RefreshToken));
            // Setting a new CSRF token might not be strictly necessary for refresh, 
            // but we can return it if needed, or just let the old one persist if it hasn't expired.
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return Unauthorized(new { detail = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        Response.Cookies.Delete("csrf_token");
        var jti = User.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
        if (!string.IsNullOrEmpty(jti) || !string.IsNullOrEmpty(request?.RefreshToken))
        {
            await mediator.Send(new LogoutCommand(jti, request?.RefreshToken));
        }
        return Ok();
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Ok(new { success = false, message = "This link is invalid or has already been used." });

        var result = await mediator.Send(new VerifyEmailCommand(token));
        return Ok(new { success = result.Success, message = result.Message, expired = result.Message == "expired" });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
            return Ok(new { sent = false });

        await mediator.Send(new ResendVerificationEmailCommand(request.Email));
        // Always return success to prevent email enumeration
        return Ok(new { sent = true });
    }
}

public record ResendVerificationRequest(string Email);

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public record RefreshRequest(string RefreshToken);
