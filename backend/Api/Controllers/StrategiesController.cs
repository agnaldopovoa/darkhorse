using Darkhorse.Application.Strategies.Commands;
using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Darkhorse.Api.Controllers;

[ApiController]
[Route("api/strategies")]
[Authorize]
public class StrategiesController(IMediator mediator, IStrategyRepository repo) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var strategies = await repo.GetByUserIdAsync(UserId, ct);
        var dtos = strategies.Select(s => new StrategyDto(
            s.Id, s.Name, s.Symbol, s.Timeframe, s.Status, s.Mode,
            s.CircuitState, s.CircuitFailures, s.Script, s.Parameters,
            s.CreatedAt, s.UpdatedAt));
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var strategy = await repo.GetByIdAsync(id, ct);
        if (strategy is null || strategy.UserId != UserId) return NotFound();

        return Ok(new StrategyDto(
            strategy.Id, strategy.Name, strategy.Symbol, strategy.Timeframe,
            strategy.Status, strategy.Mode, strategy.CircuitState, strategy.CircuitFailures,
            strategy.Script, strategy.Parameters, strategy.CreatedAt, strategy.UpdatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStrategyDto dto)
    {
        var command = new CreateStrategyCommand(
            UserId, dto.CredentialId, dto.Name, dto.Symbol, dto.Timeframe,
            dto.Script, dto.Parameters, dto.Mode, dto.MaxPositionSize, dto.MaxDailyVolume);

        var id = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStrategyDto dto)
    {
        await mediator.Send(new UpdateStrategyCommand(id, UserId, dto.Script, dto.Parameters));
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        await mediator.Send(new StartStrategyCommand(id, UserId));
        return Ok();
    }

    [HttpPost("{id:guid}/backtest")]
    public async Task<IActionResult> Backtest(Guid id, [FromBody] RunBacktestDto dto)
    {
        var jobId = await mediator.Send(new RunBacktestCommand(id, UserId, dto.StartDate, dto.EndDate));
        return Ok(new { jobId });
    }
}

public record StrategyDto(
    Guid Id, string Name, string Symbol, string Timeframe,
    string Status, string Mode, string CircuitState, int CircuitFailures,
    string Script, string Parameters,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CreateStrategyDto(Guid? CredentialId, string Name, string Symbol, string Timeframe, string Script, string Parameters, string Mode, decimal? MaxPositionSize, decimal? MaxDailyVolume);
public record UpdateStrategyDto(string Script, string Parameters);
public record RunBacktestDto(DateTimeOffset StartDate, DateTimeOffset EndDate);
