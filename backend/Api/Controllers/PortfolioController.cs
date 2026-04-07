using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Infrastructure.Cache;
using ExchangeSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Darkhorse.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioController(
    IOrderRepository orderRepo,
    RedisCacheService? cache = null) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("value")]
    public async Task<IActionResult> GetValue(CancellationToken ct)
    {
        var cacheKey = $"portfolio:{UserId}";

        // Try Redis cache first
        if (cache is not null)
        {
            var cached = await cache.GetAsync<PortfolioValueResponse>(cacheKey, ct);
            if (cached is not null) return Ok(cached);
        }

        // Get filled BUY orders
        var filledBuys = (await orderRepo.GetFilteredAsync(
            UserId, null, "filled", null, null, null, limit: 10000, ct))
            .Where(o => o.Side.Equals("BUY", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Get filled SELL orders to subtract
        var filledSells = (await orderRepo.GetFilteredAsync(
            UserId, null, "filled", null, null, null, limit: 10000, ct))
            .Where(o => o.Side.Equals("SELL", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Aggregate net quantity per symbol
        var netQuantityBySymbol = new Dictionary<string, decimal>();
        foreach (var order in filledBuys)
        {
            var qty = order.FillQuantity ?? order.Quantity;
            netQuantityBySymbol.TryAdd(order.Symbol, 0);
            netQuantityBySymbol[order.Symbol] += qty;
        }
        foreach (var order in filledSells)
        {
            var qty = order.FillQuantity ?? order.Quantity;
            if (netQuantityBySymbol.ContainsKey(order.Symbol))
                netQuantityBySymbol[order.Symbol] -= qty;
        }

        // Remove symbols with zero or negative net quantity
        var symbols = netQuantityBySymbol
            .Where(kv => kv.Value > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var breakdown = new List<PortfolioBreakdownItem>();
        decimal totalValueUsd = 0m;

        if (symbols.Count > 0)
        {
            // Use ExchangeSharp with Binance public API (no key required) — v0.4.3 style
            var exchange = ExchangeAPI.GetExchangeAPI(ExchangeName.Binance);

            foreach (var (symbol, quantity) in symbols)
            {
                try
                {
                    // Binance uses "BTCUSDT" format; convert from "BTC/USDT"
                    var exchangeSymbol = symbol.Replace("/", "").ToUpperInvariant();
                    var ticker = await exchange.GetTickerAsync(exchangeSymbol);
                    var price = ticker.Last;
                    var valueUsd = quantity * price;

                    breakdown.Add(new PortfolioBreakdownItem(symbol, quantity, price, valueUsd));
                    totalValueUsd += valueUsd;
                }
                catch
                {
                    // Symbol may not be on Binance or request failed — skip
                    breakdown.Add(new PortfolioBreakdownItem(symbol, quantity, 0m, 0m));
                }
            }
        }

        var response = new PortfolioValueResponse(totalValueUsd, breakdown, DateTimeOffset.UtcNow);

        // Cache for 30 seconds
        if (cache is not null)
            await cache.SetAsync(cacheKey, response, TimeSpan.FromSeconds(30), ct);

        return Ok(response);
    }
}

public record PortfolioBreakdownItem(string Symbol, decimal Quantity, decimal Price, decimal ValueUsd);
public record PortfolioValueResponse(decimal TotalValueUsd, IEnumerable<PortfolioBreakdownItem> Breakdown, DateTimeOffset CachedAt);
