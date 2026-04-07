using Darkhorse.Application.Brokers.Commands;
using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Darkhorse.Api.Controllers;

[ApiController]
[Route("api/brokers")]
[Authorize]
public class BrokersController(IMediator mediator, IBrokerCredentialRepository repo) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var credentials = await repo.GetByUserIdAsync(UserId, ct);
        var dtos = credentials.Select(c => new BrokerCredentialDto(
            c.Id, c.BrokerName, c.FeeRate, c.FundingRate, c.IsSandbox,
            c.Status, c.LastTestedAt, c.CreatedAt));
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrokerDto dto)
    {
        var id = await mediator.Send(new CreateBrokerCommand(
            UserId, dto.BrokerName, dto.ApiKey, dto.Secret,
            dto.FeeRate, dto.FundingRate, dto.IsSandbox));
        return CreatedAtAction(nameof(GetAll), new { }, new { id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await repo.GetByIdAsync(id, ct);
        if (existing is null || existing.UserId != UserId)
            return NotFound();

        await repo.DeleteAsync(id, ct);
        return NoContent();
    }
}

public record CreateBrokerDto(
    string BrokerName,
    string ApiKey,
    string Secret,
    decimal FeeRate,
    decimal FundingRate,
    bool IsSandbox);

public record BrokerCredentialDto(
    Guid Id,
    string BrokerName,
    decimal FeeRate,
    decimal FundingRate,
    bool IsSandbox,
    string Status,
    DateTimeOffset? LastTestedAt,
    DateTimeOffset CreatedAt);
