using Darkhorse.Domain.Exceptions;
using Darkhorse.Domain.Interfaces.Services;
using System.Diagnostics;
using System.Text.Json;

namespace Darkhorse.Infrastructure.Services;

public class StrategyExecutor : IStrategyRunner
{
    private const int MaxStdoutBytes = 1_048_576; // 1 MB
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public async Task<StrategyOutput> RunAsync(string script, StrategyContext context, CancellationToken ct = default)
    {
        var input = JsonSerializer.Serialize(new { script, parameters = context.Parameters, ohlcv = context.Ohlcv, balance = context.Balance });
        
        var psi = new ProcessStartInfo("docker",
            "run --rm --network=none --memory=256m --cpus=0.5 " +
            "--pids-limit=64 --cap-drop=ALL --security-opt=no-new-privileges:true " +
            "--read-only --tmpfs=/tmp:size=32m -i strategy-runner:latest")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) 
            ?? throw new StrategyExecutionException("Failed to start docker process.");

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(Timeout);

        try
        {
            var stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);

            if (stdout.Length > MaxStdoutBytes)
                throw new StrategyExecutionException("Output exceeded 1 MB limit");

            var output = JsonSerializer.Deserialize<StrategyOutput>(stdout, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (output is null || !output.IsValid())
                throw new StrategyExecutionException("Invalid strategy output format.");

            return output;
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { /* Ignore */ }
            throw new StrategyExecutionException("Strategy execution timed out after 30 seconds.");
        }
        catch (JsonException ex)
        {
            throw new StrategyExecutionException($"Failed to parse container JSON output: {ex.Message}", ex);
        }
    }
}
