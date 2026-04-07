using Darkhorse.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Darkhorse.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(
    IStrategyRepository strategyRepo,
    IOrderRepository orderRepo) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var strategies = (await strategyRepo.GetByUserIdAsync(UserId, ct)).ToList();

        var activeStrategies = strategies.Count(s => s.Status == "running");
        var openCircuits = strategies.Count(s => s.CircuitState == "OPEN");
        var halfOpenCircuits = strategies.Count(s => s.CircuitState == "HALF-OPEN");

        var systemHealthState = openCircuits >= 2 ? "CRITICAL"
            : openCircuits == 1 ? "WARN"
            : halfOpenCircuits > 0 ? "WARN"
            : "OK";

        var healthDetails = openCircuits == 0 && halfOpenCircuits == 0
            ? "All strategies nominal"
            : openCircuits > 0
                ? $"{openCircuits} circuit{(openCircuits > 1 ? "s" : "")} open — strategy auto-paused"
                : $"{halfOpenCircuits} circuit{(halfOpenCircuits > 1 ? "s" : "")} testing";

        var recentOrders = await orderRepo.GetFilteredAsync(
            UserId, null, null, null, null, null, limit: 5, ct);

        var orderDtos = recentOrders.Select(o => new RecentOrderDto(
            o.Id, o.Symbol, o.Side, o.FillPrice, o.Status, o.CreatedAt));

        var totalOrders = await orderRepo.GetFilteredAsync(UserId, null, null, null, null, null, limit: 10000, ct);

        return Ok(new
        {
            activeStrategies,
            totalOrders = totalOrders.Count(),
            systemHealth = new
            {
                state = systemHealthState,
                openCircuits,
                halfOpenCircuits,
                details = healthDetails
            },
            recentOrders = orderDtos
        });
    }
}

public record RecentOrderDto(
    Guid Id,
    string Symbol,
    string Side,
    decimal? FillPrice,
    string Status,
    DateTimeOffset CreatedAt);
