using Darkhorse.Application.Strategies.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Darkhorse.Api.Controllers;

[ApiController]
[Route("api/strategies")]
[Authorize]
public class StrategiesController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStrategyDto dto)
    {
        var command = new CreateStrategyCommand(
            UserId, dto.CredentialId, dto.Name, dto.Symbol, dto.Timeframe, 
            dto.Script, dto.Parameters, dto.Mode, dto.MaxPositionSize, dto.MaxDailyVolume);
        
        var id = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStrategyDto dto)
    {
        await mediator.Send(new UpdateStrategyCommand(id, UserId, dto.Script, dto.Parameters));
        return NoContent();
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        await mediator.Send(new StartStrategyCommand(id, UserId));
        return Ok();
    }

    [HttpPost("{id}/backtest")]
    public async Task<IActionResult> Backtest(Guid id, [FromBody] RunBacktestDto dto)
    {
        var jobId = await mediator.Send(new RunBacktestCommand(id, UserId, dto.StartDate, dto.EndDate));
        return Ok(new { jobId });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        return Ok(new { id, message = "Query endpoint scaffold" });
    }
}

public record CreateStrategyDto(Guid? CredentialId, string Name, string Symbol, string Timeframe, string Script, string Parameters, string Mode, decimal? MaxPositionSize, decimal? MaxDailyVolume);
public record UpdateStrategyDto(string Script, string Parameters);
public record RunBacktestDto(DateTimeOffset StartDate, DateTimeOffset EndDate);
