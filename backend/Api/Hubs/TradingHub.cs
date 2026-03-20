using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Darkhorse.Api.Hubs;

// Strongly typed interface for clients according to architecture §19.2
public interface ITradingClient
{
    Task OnStrategyUpdate(Guid strategyId, string status, string lastSignal, decimal pnl);
    Task OnOrderFill(Guid orderId, string symbol, string side, decimal fillPrice);
    Task OnNotification(Guid notificationId, string title, string type);
    Task OnBacktestProgress(string taskId, int percent, DateTimeOffset currentDate);
    Task OnBacktestComplete(string taskId, decimal pnl, int totalTrades, decimal sharpe);
}

[Authorize]
public class TradingHub : Hub<ITradingClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            // Group connections by UserId to broadcast to all of a user's devices automatically
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
