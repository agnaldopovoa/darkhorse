# Architecture Document: Darkhorse Crypto Trading Platform (.NET)

**Version:** 1.0
**Status:** Initial — Ported from Python v3.0 architecture

---

## Table of Contents

1. System Overview
2. Technology Stack Decisions
3. Backend Clean Architecture
4. Security Architecture
5. Authentication & Session Management
6. Rate Limiting
7. Broker Integration Layer
8. Strategy Execution — Sandboxed Python DSL
9. Paper Trading Mode
10. Backtesting Engine
11. Error Circuit Breaker
12. Task Queue
13. Redis Infrastructure
14. Structured Logging
15. Data Model
16. User Interface Screens
17. Deployment
18. API Contract
19. WebSocket Protocol
20. Notification System

---

## 1. System Overview

A robust, secure, and highly available web application that allows users to write custom trading strategies in a sandboxed Python DSL, backtest them against stored historical OHLCV data, run them in a simulated paper trading environment, and deploy live automated trading bots across multiple cryptocurrency brokers (Binance, KuCoin, Coinbase).

The backend is built on **.NET 8 LTS** (ASP.NET Core) for type safety, performance, and developer familiarity. Strategy scripts remain in **Python** for simplicity and are executed inside isolated Docker containers. The system is designed to run on free-tier cloud infrastructure using Docker Compose.

---

## 2. Technology Stack Decisions

| Layer | Technology | Rationale |
|---|---|---|
| Frontend | React (SPA) | Reactive UI, rich component ecosystem |
| Backend API | .NET 8 LTS / ASP.NET Core | High performance, compile-time safety, mature ecosystem |
| ORM | Entity Framework Core | Code-first migrations, LINQ queries, PostgreSQL provider |
| Database | PostgreSQL | Relational integrity for users, orders, strategies |
| Cache | Redis | Ticker TTL cache, circuit breaker state, JWT revocation |
| Task Queue | Hangfire + PostgreSQL | Built-in dashboard, cron scheduling, no extra infrastructure |
| Broker Integration | ExchangeSharp (MIT) | .NET-native library supporting Binance, KuCoin, Coinbase |
| Strategy Scripting | Sandboxed Python DSL | Docker-isolated execution with `runner.py` (see §7) |
| Code Editor (UI) | Monaco Editor | VSCode-quality editor, Python syntax highlighting |
| Structured Logging | Serilog + stdout | JSON structured logs, platform-native log capture |
| Deployment | Docker + Docker Compose | Consistent environments across all free-tier hosts |

---

## 3. Backend Clean Architecture

### 3.1 Principles
The backend uses **Clean Architecture** to ensure separation of concerns, independance from UI/DB/frameworks, and high testability. Dependencies always point inwards toward the Domain.

### 3.2 Layers

| Layer | Project | Contains | Dependencies |
|---|---|---|---|
| **Domain** | `Darkhorse.Domain` | Entities (`User`, `Strategy`, `Order`), Enums, Exceptions, Repository Interfaces | None |
| **Application** | `Darkhorse.Application` | Use Cases, CQRS (MediatR), DTOs, Validation, Interfaces for external services (e.g. `IBrokerAPI`) | `Domain` |
| **Infrastructure** | `Darkhorse.Infrastructure` | EF Core `DbContext`, Hangfire jobs, ExchangeSharp implementations, Redis Cache, Security services | `Application`, `Domain` |
| **Presentation** | `Darkhorse.Api` | ASP.NET Core Controllers, SignalR Hubs, Middleware, Dependency Injection Setup | `Application`, `Infrastructure` |
| **Worker** | `Darkhorse.Worker` | Background service entry point for Hangfire processes and Docker API integration | `Application`, `Infrastructure` |

### 3.3 Flow Example (Place Order)
1. **Controller (Presentation)** receives HTTP request, routes to MediatR command.
2. **Command Handler (Application)** validates request, retrieves `Strategy` entity via generic `IStrategyRepository`.
3. **Broker Service (Application Interface)** requests order placement.
4. **ExchangeSharp Adapter (Infrastructure)** physically sends the network request to Binance.
5. **Command Handler** updates `Order` entity, tells `IOrderRepository` to save.
6. **EF Core Repository (Infrastructure)** commits to PostgreSQL.

---

## 4. Security Architecture

### 4.1 Transport Security

- All traffic is served exclusively over **HTTPS/TLS**. Plain HTTP requests are rejected at the reverse proxy (Caddy or platform ingress) with a 301 redirect.
- HSTS headers are set on all responses: `Strict-Transport-Security: max-age=63072000; includeSubDomains`.

### 4.2 Password Hashing

Passwords are hashed using **Argon2id** via the `Konscious.Security.Cryptography` NuGet package.

- Parameters: `DegreeOfParallelism=4`, `MemorySize=65536` (64 MB), `Iterations=3`, `HashLength=32`.

```csharp
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

public class PasswordService
{
    public (byte[] hash, byte[] salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            MemorySize = 65536,
            Iterations = 3
        };
        return (argon2.GetBytes(32), salt);
    }

    public bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            MemorySize = 65536,
            Iterations = 3
        };
        return CryptographicOperations.FixedTimeEquals(argon2.GetBytes(32), hash);
    }
}
```

### 4.3 Broker Credential Storage

Both the API key and API secret are sensitive credentials. **Neither is stored in plaintext.** Both are encrypted with AES-256-GCM before touching the database.

**Encryption flow:**

1. User submits API key + secret via HTTPS form.
2. Backend encrypts **both** using **AES-256-GCM** before storage.
3. A unique random **nonce (96-bit)** is generated per encryption operation.
4. The **master encryption key** lives exclusively in a server environment variable (`MASTER_ENCRYPTION_KEY`). It is never committed to source control.
5. On order execution, the backend decrypts credentials in-memory, uses them, immediately discards them, and **logs the decryption event to `audit_logs`** (see §3.7).

```csharp
using System.Security.Cryptography;

public class CredentialEncryption
{
    public static (byte[] nonce, byte[] ciphertext, byte[] tag) Encrypt(
        string plaintext, byte[] masterKey)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); // 96-bit
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16]; // 128-bit auth tag

        using var aes = new AesGcm(masterKey, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return (nonce, cipher, tag);
    }

    public static string Decrypt(
        byte[] nonce, byte[] ciphertext, byte[] tag, byte[] masterKey)
    {
        var plain = new byte[ciphertext.Length];
        using var aes = new AesGcm(masterKey, tagSizeInBytes: 16);
        aes.Decrypt(nonce, ciphertext, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
```

### 4.4 Master Key Rotation

Key rotation uses a **versioned key scheme**. Each `broker_credentials` row stores a `key_version` integer. When rotating:

1. Generate new master key, store as next version in the environment.
2. Run a background Hangfire job that re-encrypts all secrets under the new key version.
3. Retire the old key version after all rows are migrated.

### 4.5 CORS Policy

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(','))
              .AllowCredentials()
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Authorization", "Content-Type", "X-CSRF-Token");
    });
});
```

### 4.6 CSRF Protection

Refresh tokens use `SameSite=Lax` cookies. A **double-submit cookie** pattern protects against CSRF on mutating endpoints:

1. On login, the server sets a `csrf_token` cookie (`Secure; SameSite=Lax`, **not** `HttpOnly`).
2. The React frontend reads the cookie and sends it as the `X-CSRF-Token` header.
3. Middleware compares header vs. cookie. Mismatch → `403 Forbidden`.

```csharp
public class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsDelete(context.Request.Method))
        {
            var cookieToken = context.Request.Cookies["csrf_token"];
            var headerToken = context.Request.Headers["X-CSRF-Token"].FirstOrDefault();

            if (string.IsNullOrEmpty(cookieToken) || cookieToken != headerToken)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { detail = "CSRF validation failed" });
                return;
            }
        }
        await _next(context);
    }
}
```

### 4.7 Secret Access Audit

Every decryption of a broker credential is logged to `audit_logs`, creating a forensic trail.

---

## 5. Authentication & Session Management

### 5.1 JWT Strategy

The system uses a **dual-token JWT scheme** via `Microsoft.AspNetCore.Authentication.JwtBearer`:

| Token | Lifetime | Storage | Purpose |
|---|---|---|---|
| Access Token | 15 minutes | Memory (JS variable) | Authorise API requests |
| Refresh Token | 7 days | HttpOnly cookie | Obtain new access tokens |

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSecret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### 5.2 Token Flow

```
1. POST /auth/login  →  { access_token }  +  Set-Cookie: refresh_token=...
2. Client stores access_token in memory (never localStorage)
3. On access_token expiry → POST /auth/refresh (sends cookie automatically)
4. Server validates refresh_token → issues new access_token
5. POST /auth/logout → server invalidates refresh_token in DB blacklist
```

### 5.3 Refresh Token Revocation

Refresh tokens are stored in a Redis set keyed by `jti` (JWT ID). On logout or force-logout, the token's `jti` is added to a revocation list. The `/auth/refresh` endpoint checks this list before issuing a new access token.

### 5.4 Password Change & Force Logout

When a user changes their password:
1. The new password is re-hashed with Argon2id.
2. **All active refresh tokens for that user are revoked** immediately.

### 5.5 User Registration (Multi-Tenant)

The application is **multi-tenant** — any user can register an account.

1. `POST /auth/register` with `{ email, password }`.
2. Server validates email format and password strength (min 12 chars, at least 1 number + 1 special).
3. Password is hashed with Argon2id and stored.
4. A verification email is sent with a signed token (URL-safe, 24h expiry).
5. `GET /auth/verify?token=...` activates the account (`is_active = true`).
6. Until verified, login returns `403 Account not verified`.

Rate limit: `POST /auth/register` is limited to **3 requests / minute / IP**.

---

## 6. Rate Limiting

Rate limiting is applied using `AspNetCoreRateLimit` NuGet package.

### 6.1 Rate Limit Rules

| Endpoint | Limit | Window | Rationale |
|---|---|---|---|
| `POST /auth/login` | 5 requests | 1 minute / IP | Prevent brute-force |
| `POST /auth/register` | 3 requests | 1 minute / IP | Prevent spam registration |
| `POST /auth/refresh` | 20 requests | 1 minute / IP | Prevent token farming |
| `POST /strategies/{id}/execute` | 10 requests | 1 minute / user | Protect broker API quotas |
| `GET /brokers/{id}/ticker` | 30 requests | 1 minute / user | Complement Redis cache |
| All other endpoints | 120 requests | 1 minute / user | General abuse prevention |

```csharp
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new() { Endpoint = "POST:/auth/login", Period = "1m", Limit = 5 },
        new() { Endpoint = "POST:/auth/register", Period = "1m", Limit = 3 },
        new() { Endpoint = "*", Period = "1m", Limit = 120 }
    };
});
```

Rate limit violations return HTTP `429 Too Many Requests` with a `Retry-After` header.

---

## 7. Broker Integration Layer

### 7.1 ExchangeSharp Adapter

All broker communication is abstracted behind a common interface implemented via **ExchangeSharp** (MIT license). Supported exchanges: **Binance, KuCoin, Coinbase**.

```csharp
using ExchangeSharp;

public class BrokerAdapter : IDisposable
{
    private readonly ExchangeAPI _exchange;

    public static async Task<BrokerAdapter> CreateAsync(
        string exchangeName, string apiKey, string secret, bool sandbox = false)
    {
        var exchange = await ExchangeAPI.GetExchangeAPIAsync(exchangeName);
        exchange.Credentials = new ExchangeCredentials
        {
            PublicApiKey = apiKey.ToSecureString(),
            PrivateApiKey = secret.ToSecureString()
        };
        if (sandbox) exchange.IsSandbox = true;
        return new BrokerAdapter(exchange);
    }

    private BrokerAdapter(ExchangeAPI exchange) => _exchange = exchange;

    public async Task<IEnumerable<ExchangeMarket>> GetMarketsAsync()
        => await _exchange.GetMarketSymbolsMetadataAsync();

    public async Task<ExchangeTicker> GetTickerAsync(string symbol)
        => await _exchange.GetTickerAsync(symbol);

    public async Task<IEnumerable<MarketCandle>> GetOhlcvAsync(
        string symbol, int periodSeconds, DateTime? startDate)
        => await _exchange.GetCandlesAsync(symbol, periodSeconds, startDate);

    public async Task<ExchangeOrderResult> PlaceOrderAsync(
        string symbol, string side, decimal amount, decimal? price = null)
    {
        var request = new ExchangeOrderRequest
        {
            MarketSymbol = symbol,
            Amount = amount,
            Price = price,
            IsBuy = side.Equals("BUY", StringComparison.OrdinalIgnoreCase),
            OrderType = price.HasValue ? OrderType.Limit : OrderType.Market
        };
        return await _exchange.PlaceOrderAsync(request);
    }

    public async Task CancelOrderAsync(string orderId, string symbol)
        => await _exchange.CancelOrderAsync(orderId, symbol);

    public void Dispose() => _exchange?.Dispose();
}
```

### 7.2 Ticker Cache

Every call to `GetTickerAsync()` is wrapped in a Redis TTL cache with a **5-second expiry**.

```csharp
public async Task<ExchangeTicker> GetTickerCachedAsync(string symbol, string exchange)
{
    var key = $"ticker:{exchange}:{symbol}";
    var cached = await _redis.StringGetAsync(key);
    if (cached.HasValue)
        return JsonSerializer.Deserialize<ExchangeTicker>(cached!);

    var ticker = await _adapter.GetTickerAsync(symbol);
    await _redis.StringSetAsync(key, JsonSerializer.Serialize(ticker), TimeSpan.FromSeconds(5));
    return ticker;
}
```

---

## 8. Strategy Execution — Sandboxed Python DSL

### 8.1 Security Model

User-submitted strategy scripts are **untrusted code**. Each strategy execution runs inside a **dedicated, ephemeral Docker container** with all network, filesystem, and process capabilities stripped. The strategy runner remains in **Python** for simplicity and ecosystem compatibility.

### 8.2 Container Isolation Specification

```yaml
runtime: runc
network_mode: none
read_only: true
tmpfs: /tmp:size=32m
mem_limit: 256m
cpus: 0.5
pids_limit: 64
cap_drop: ALL
security_opt:
  - no-new-privileges:true
user: "1000:1000"
```

### 8.3 Execution Protocol

Strategy containers are spawned **exclusively by the Hangfire worker** — never by the API service. The API service does not mount the Docker socket. Communication is via **JSON over stdin/stdout**:

1. Hangfire job serialises execution context to JSON: `{ "script": "...", "ohlcv": [...], "balance": {...}, "parameters": {...} }`.
2. Context is piped into the container's stdin.
3. Inside the container, the fixed **`runner.py`** harness reads stdin, executes the user script in a restricted scope, and writes the result to stdout.
4. The container exits. The worker reads stdout (capped at **1 MB**) and validates against a strict model.
5. A **30-second hard timeout** kills the container.

### 8.4 Runner Harness (`runner.py`)

The strategy container image contains a fixed, immutable `runner.py` (Python). The user's script arrives as a field inside the JSON payload on stdin.

```python
# runner.py — baked into the strategy-runner Docker image (Python Alpine)
import sys, json, statistics

def main():
    payload = json.loads(sys.stdin.read())

    safe_builtins = {
        "abs": abs, "min": min, "max": max, "sum": sum, "len": len,
        "range": range, "round": round, "float": float, "int": int,
        "bool": bool, "str": str, "list": list, "dict": dict,
        "True": True, "False": False, "None": None,
    }

    scope = {
        "__builtins__": safe_builtins,
        "ohlcv": payload["ohlcv"],
        "balance": payload["balance"],
        "params": payload["parameters"],
        "statistics": statistics,
        "signal": "HOLD",
        "quantity": 0.0,
        "reason": "",
    }

    exec(payload["script"], scope)

    result = {
        "signal": scope.get("signal", "HOLD"),
        "quantity": scope.get("quantity", 0.0),
        "reason": str(scope.get("reason", ""))[:500],
    }
    sys.stdout.write(json.dumps(result))

if __name__ == "__main__":
    main()
```

### 8.5 Backtest Harness (`backtest_runner.py`)

For backtesting, efficiency is paramount. Instead of the .NET backend iterating through historical candles and spawning a container for each tick (or calculating simulated fills itself), the entire backtest logic is evaluated inside a dedicated `backtest_runner.py`.

The backend passes the full block of OHLCV data. The Python harness iterates through it, maintains a virtual balance, evaluates the user's script on each window, simulates execution at the next open price, computes the final P&L, and returns the aggregate metrics.

```python
# backtest_runner.py
import sys, json, statistics

def main():
    payload = json.loads(sys.stdin.read())
    history = payload["ohlcv"]
    script = payload["script"]
    
    # ... setup safe_builtins ...
    
    trades = []
    initial_balance = payload["balance"].get("USDT", 10000)
    current_balance = initial_balance
    position = 0.0

    # Iterating internally is orders of magnitude faster than API/Docker roundtrips
    for i in range(50, len(history)):
        window = history[max(0, i-200):i] # Rolling window
        
        scope = {
            "__builtins__": safe_builtins,
            "ohlcv": window,
            "balance": {"USDT": current_balance},
            "params": payload["parameters"],
            "statistics": statistics,
            "signal": "HOLD",
            "quantity": 0.0,
        }
        
        try:
            exec(script, scope)
        except Exception as e:
            sys.stdout.write(json.dumps({"error": str(e)}))
            return
            
        signal = scope.get("signal", "HOLD")
        qty = scope.get("quantity", 0.0)
        
        # Simulate Fill at next candle open (simplistic model)
        if i + 1 < len(history) and signal in ["BUY", "SELL"]:
            fill_price = history[i+1][1] # Open price
            
            if signal == "BUY" and current_balance >= (qty * fill_price):
                current_balance -= (qty * fill_price)
                position += qty
                trades.append({"type": "BUY", "price": fill_price, "qty": qty, "time": history[i+1][0]})
                
            elif signal == "SELL" and position >= qty:
                current_balance += (qty * fill_price)
                position -= qty
                trades.append({"type": "SELL", "price": fill_price, "qty": qty, "time": history[i+1][0]})

    # Mark to market remaining position at final close price
    final_price = history[-1][4]
    final_equity = current_balance + (position * final_price)
    pnl_percentage = ((final_equity - initial_balance) / initial_balance) * 100

    result = {
        "initial_balance": initial_balance,
        "final_equity": final_equity,
        "pnl_percentage": round(pnl_percentage, 2),
        "total_trades": len(trades),
        "trade_log": trades
    }
    
    sys.stdout.write(json.dumps(result))

if __name__ == "__main__":
    main()
```

### 8.5 Output Validation

The .NET worker validates every container output:

```csharp
public record StrategyOutput
{
    [JsonPropertyName("signal")]
    public string Signal { get; init; } = "HOLD";  // BUY | SELL | HOLD

    [JsonPropertyName("quantity")]
    public double Quantity { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    public bool IsValid() =>
        Signal is "BUY" or "SELL" or "HOLD"
        && Quantity >= 0 && Quantity <= 1_000_000
        && (Reason?.Length ?? 0) <= 500;
}
```

### 8.6 Container Spawning (Hangfire Worker Only)

```csharp
public class StrategyExecutor
{
    private const int MaxStdoutBytes = 1_048_576; // 1 MB
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public async Task<StrategyOutput> RunAsync(string script, object context)
    {
        var input = JsonSerializer.Serialize(new { script, context });
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

        using var process = Process.Start(psi)!;
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        using var cts = new CancellationTokenSource(Timeout);
        var stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);

        if (stdout.Length > MaxStdoutBytes)
            throw new StrategyExecutionException("Output exceeded 1 MB limit");

        var output = JsonSerializer.Deserialize<StrategyOutput>(stdout)!;
        if (!output.IsValid())
            throw new StrategyExecutionException("Invalid strategy output");

        return output;
    }
}
```

### 8.7 Example Strategy Script

```python
# Available variables: ohlcv, balance, params, statistics, signal, quantity, reason

closes = [c[4] for c in ohlcv]

def rsi(prices, period=14):
    gains, losses = [], []
    for i in range(1, len(prices)):
        delta = prices[i] - prices[i-1]
        gains.append(max(delta, 0))
        losses.append(max(-delta, 0))
    avg_gain = statistics.mean(gains[-period:])
    avg_loss = statistics.mean(losses[-period:])
    rs = avg_gain / avg_loss if avg_loss else float('inf')
    return 100 - (100 / (1 + rs))

current_rsi = rsi(closes, params.get("rsi_period", 14))

if current_rsi < 30:
    signal = "BUY"
elif current_rsi > 70:
    signal = "SELL"
else:
    signal = "HOLD"
```

---

## 9. Paper Trading Mode

### 9.1 Overview

Paper trading allows users to test strategies against **live real-time market prices** without placing real orders. It uses the exchange's official **sandbox/testnet environment** (where available via ExchangeSharp's `IsSandbox = true` flag) or a simulated internal order book when the exchange does not provide a testnet.

### 9.2 Dedicated UI Page

The Paper Trading page is a standalone screen accessible from the main navigation. It mirrors the Live Strategies Dashboard but displays a clear visual indicator ("Paper Trading — No real funds at risk") at all times.

### 9.3 Implementation

```csharp
public async Task<ExchangeOrderResult> PlaceOrderAsync(
    string symbol, string side, decimal amount, decimal? price = null)
{
    if (_mode == "paper" && !_exchange.IsSandbox)
    {
        // Route to internal paper order book if exchange lacks sandbox
        return await _paperOrderBook.FillAsync(symbol, side, amount, price);
    }
    
    // Live / Sandbox native execution
    var request = new ExchangeOrderRequest { /* ... */ };
    return await _exchange.PlaceOrderAsync(request);
}
```

Paper orders are stored in the same `orders` table with `mode = 'paper'` to keep the schema unified and enable direct comparison queries.

---

## 10. Backtesting Engine

### 10.1 Data Source & Retrieval Endpoint

Backtesting runs exclusively against **stored OHLCV data** from the `data_history` PostgreSQL table (which acts as the persistent cold store) and **Redis** (which acts as the fast hot tier for frequently queried backtest ranges). It never calls live broker APIs during a backtest run, ensuring reproducibility and preventing accidental rate-limiting.

To populate the backtesting UI with available data ranges, or for developers to pull datasets manually, the API exposes the following endpoint:
`GET /data/history?broker={id}&symbol={symbol}&timeframe={1h}`
1. The endpoint first checks **Redis** (`history:{broker}:{symbol}:{timeframe}`).
2. If a cache miss occurs, it queries PostgreSQL (`data_history`), caches the result in Redis, and returns the payload.

### 10.2 Backtest Execution Flow

To avoid the overhead of spawning thousands of containers (one per OHLCV window), backtesting uses a **single long-lived container** that iterates over all windows internally.

```
User clicks "Backtest" in Strategy Editor
  → Selects: broker, symbol, timeframe, date range
  → Hangfire background job is enqueued

Hangfire worker (Infrastructure Layer):
  1. Queries EF Core `data_history` for the requested symbol/timeframe/range
  2. Serialises ALL OHLCV data + script + params into a single JSON payload
  3. Spawns ONE long-lived Python container explicitly running `backtest_runner.py` (instead of `runner.py`)
  4. The container iterates over the historical data internally, simulates order fills, and tracks balance
  5. Container calculates final performance metrics (P&L, win rate, max drawdown, Sharpe ratio, total trades)
  6. Container outputs a unified JSON result array (metrics + trade history) to stdout
  7. Worker validates output and stores results in `executions` table (mode = 'backtest')
  8. Notifies frontend via SignalR WebSocket

Frontend:
  → Renders equity curve chart
  → Displays metrics summary
```

### 10.3 Daily Historical Data Extraction (Hangfire Cron)

To ensure the backtesting engine always has up-to-date data, a **daily scheduled task** runs during off-peak hours (e.g., 00:05 UTC) to extract the previous day's OHLCV candles.

This is implemented using Hangfire's `RecurringJob`:

```csharp
// Bootstrapped in worker Program.cs
RecurringJob.AddOrUpdate<IDataExtractionService>(
    "daily-ohlcv-extraction",
    service => service.ExtractYesterdayDataAsync(),
    "5 0 * * *" // 00:05 UTC every day
);
```

**Extraction Flow:**
1. The job loops through all active `(Broker, Symbol, Timeframe)` combinations used by saved strategies.
2. It calls the broker's API via ExchangeSharp to fetch merely the last 24 hours of candles.
3. The new candles are appended/upserted into the PostgreSQL `data_history` table.
4. **Cache Invalidation:** The job explicitly deletes the corresponding `history:{broker}:{symbol}:{timeframe}` keys from **Redis**.
5. The next time a user requests a backtest, the system will serve a fresh, up-to-date dataset from PostgreSQL and re-warm the Redis cache.

---

## 11. Error Circuit Breaker

### 11.1 Purpose

The circuit breaker prevents a broken or misbehaving strategy from repeatedly failing, spamming the broker API, or placing erroneous orders. It automatically pauses a strategy after a configurable number of consecutive failures.

### 11.2 Implementation (Polly)

In .NET, the circuit breaker pattern is implemented using **Polly**, a standard resilience and transient-fault-handling library. The state is backed by Redis to ensure it is shared across all worker instances.

```csharp
using Polly;
using Polly.CircuitBreaker;

public class DistributedCircuitBreakerFactory
{
    public AsyncCircuitBreakerPolicy CreatePolicy(string strategyId, int threshold, TimeSpan cooldown)
    {
        return Policy
            .Handle<StrategyExecutionException>()
            .Or<ExchangeAPIException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: threshold,
                durationOfBreak: cooldown,
                onBreak: async (exception, timespan) => {
                    await NotifyUserAsync(strategyId, "Strategy Paused", exception.Message);
                    await LogCircuitStateAsync(strategyId, "OPEN");
                },
                onReset: async () => {
                    await LogCircuitStateAsync(strategyId, "CLOSED");
                },
                onHalfOpen: async () => {
                    await LogCircuitStateAsync(strategyId, "HALF-OPEN");
                }
            );
    }
}
```

---

## 12. Task Queue

### 12.1 Stack

**Hangfire** is used as the task queue. Unlike the Python architecture which required Redis for Celery, Hangfire natively supports **PostgreSQL** as its storage backend (`Hangfire.PostgreSql`). This reduces architectural complexity by using the existing database for persistent, transactional task queues.

### 12.2 Task Types

| Task | Trigger | Description |
|---|---|---|
| `ExecuteStrategyJob` | Scheduler (cron) | Run one strategy tick; feed signal to order manager |
| `RunBacktestJob` | User request | Execute full backtest over historical data range |
| `FetchOhlcvJob` | Daily cron | Fetch and store new OHLCV candles into `data_history` |
| `PlaceOrderJob` | Strategy signal | Submit order to broker via ExchangeSharp adapter |
| `ReconcileOrdersJob` | Worker startup | Check broker for status of any orders stuck in 'submitted' state |
| `NotifyUserJob` | Circuit breaker / errors | Send email or in-app notification |

### 12.3 Configuration

```csharp
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer(options => {
    options.WorkerCount = Environment.ProcessorCount * 2;
});
```

---

## 13. Redis Infrastructure

In the .NET architecture, Redis is **no longer used as a message broker** (Hangfire handles that). It is used strictly for high-speed, transient distributed caching. All Redis connections use authentication (`requirepass`).

| Cache | Key Pattern | TTL | Purpose |
|---|---|---|---|
| Ticker data | `ticker:{exchange}:{symbol}` | 5s | Prevent repeated price API calls |
| Market list | `markets:{exchange}` | 300s | Avoid re-fetching available symbols |
| Balance | `balance:{user_id}:{exchange}` | 30s | Reduce balance API calls across strategy ticks |
| Rate limit counters | `ratelimit:{endpoint}:{ip}` | 60s | Track request counts per window |
| Circuit breaker state | `cb:{strategy_id}:state` | — | Fast distributed state lookup for Polly |
| Refresh token blacklist | `revoked:{jti}` | 7 days | JWT revocation lookup |

---

## 14. Structured Logging

### 14.1 Approach

**Serilog + stdout** is used for all logging. Every log entry is emitted as a single JSON line to stdout. Free-tier platforms (Railway, Google Cloud Run, Fly.io) capture stdout automatically and expose it in their built-in log viewers. No log aggregation infrastructure is required.

### 14.2 Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Darkhorse.Trading")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

### 14.3 Correlation ID

Middleware automatically generates a `RequestId` (`HttpContext.TraceIdentifier`) and pushes it into the Serilog `LogContext`, linking all log lines produced by a single request or strategy execution tick.

---

## 15. Data Model

### 15.1 Entity Overview

```
users
  ├── broker_credentials  (one user → many brokers)
  ├── strategies          (one user → many strategies)
  │     ├── strategy_versions (one strategy → many script revisions)
  │     ├── executions    (one strategy → many execution logs)
  │     └── orders        (one strategy → many orders)
  ├── notifications       (in-app notifications)
  └── audit_logs          (all user actions)

data_history              (global OHLCV store, shared across users)
```

### 15.2 Table Definitions (EF Core Managed)

#### `users`
```sql
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email           VARCHAR(255) UNIQUE NOT NULL,
    password_hash   TEXT NOT NULL,              -- Argon2id output
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);
```

#### `broker_credentials`
```sql
CREATE TABLE broker_credentials (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    broker_name      VARCHAR(64) NOT NULL,        -- e.g. "binance", "kraken"
    apikey_nonce     BYTEA NOT NULL,              -- AES-GCM nonce for API key
    apikey_cipher    BYTEA NOT NULL,              -- AES-256-GCM encrypted API key
    secret_nonce     BYTEA NOT NULL,              -- AES-GCM nonce for secret
    secret_cipher    BYTEA NOT NULL,              -- AES-256-GCM encrypted secret
    key_version      INTEGER NOT NULL DEFAULT 1,  -- for key rotation
    fee_rate         NUMERIC(8, 6) DEFAULT 0,     -- user-configured fee % per trade
    funding_rate     NUMERIC(8, 6) DEFAULT 0,     -- user-configured funding rate
    is_sandbox       BOOLEAN DEFAULT FALSE,       -- True = testnet/paper endpoint
    status           VARCHAR(32) DEFAULT 'active', -- active | revoked | error
    last_tested_at   TIMESTAMPTZ,
    created_at       TIMESTAMPTZ DEFAULT now()
);
```

#### `strategies`
```sql
CREATE TABLE strategies (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    credential_id       UUID REFERENCES broker_credentials(id),
    name                VARCHAR(128) NOT NULL,
    symbol              VARCHAR(32) NOT NULL,       -- e.g. "BTC/USDT"
    timeframe           VARCHAR(8) NOT NULL,        -- e.g. "1h", "15m"
    script              TEXT NOT NULL,              -- current Python DSL code
    script_version      INTEGER DEFAULT 1,          -- incremented on edit
    parameters          JSONB DEFAULT '{}',         -- user-defined params
    mode                VARCHAR(16) DEFAULT 'paper', -- live | paper | backtest
    status              VARCHAR(16) DEFAULT 'paused', -- running | paused | error
    max_position_size   NUMERIC(24, 8),             -- hard cap per order
    max_daily_volume    NUMERIC(24, 8),             -- daily aggregate cap
    circuit_state       VARCHAR(16) DEFAULT 'CLOSED', -- CLOSED | OPEN | HALF-OPEN
    circuit_failures    INTEGER DEFAULT 0,
    circuit_opened_at   TIMESTAMPTZ,
    schedule_interval   INTEGER DEFAULT 60,        -- seconds between ticks
    created_at          TIMESTAMPTZ DEFAULT now(),
    updated_at          TIMESTAMPTZ DEFAULT now()
);
```

#### `orders`
```sql
CREATE TABLE orders (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    strategy_id         UUID NOT NULL REFERENCES strategies(id),
    user_id             UUID NOT NULL REFERENCES users(id),
    broker_order_id     VARCHAR(128),              -- exchange confirmation ID
    symbol              VARCHAR(32) NOT NULL,
    side                VARCHAR(8) NOT NULL,        -- BUY | SELL
    order_type          VARCHAR(16) DEFAULT 'market', -- market | limit
    quantity            NUMERIC(24, 8) NOT NULL,
    requested_price     NUMERIC(24, 8),
    fill_price          NUMERIC(24, 8),
    fill_quantity       NUMERIC(24, 8),
    status              VARCHAR(16) NOT NULL,        -- submitted | pending | filled | cancelled | rejected
    mode                VARCHAR(16) NOT NULL,        -- live | paper | backtest
    fees                NUMERIC(24, 8),
    fee_currency        VARCHAR(16),
    signal_reason       TEXT,                       -- human-readable signal justification
    created_at          TIMESTAMPTZ DEFAULT now(),
    filled_at           TIMESTAMPTZ
);
```

#### `executions`
```sql
CREATE TABLE executions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    strategy_id     UUID NOT NULL REFERENCES strategies(id),
    script_version  INTEGER,                       -- which script version produced this signal
    signal          VARCHAR(8) NOT NULL,           -- BUY | SELL | HOLD
    signal_reason   TEXT,
    mode            VARCHAR(16) NOT NULL,           -- live | paper | backtest
    context_snapshot JSONB,                        -- OHLCV window + balance snapshot
    output_raw      JSONB,                         -- raw container stdout output
    error_message   TEXT,                          -- populated on failure
    duration_ms     INTEGER,                       -- execution time in milliseconds
    container_exit_code INTEGER,
    created_at      TIMESTAMPTZ DEFAULT now()
);
```

#### `audit_logs`
```sql
CREATE TABLE audit_logs (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID REFERENCES users(id),
    action      VARCHAR(64) NOT NULL,  -- e.g. "broker_created", "strategy_started", "password_changed"
    entity_type VARCHAR(32),           -- e.g. "strategy", "order", "broker_credential"
    entity_id   UUID,
    ip_address  INET,
    user_agent  TEXT,
    metadata    JSONB DEFAULT '{}',    -- additional context (non-sensitive)
    created_at  TIMESTAMPTZ DEFAULT now()
);
```

#### `data_history`
```sql
CREATE TABLE data_history (
    id          BIGSERIAL PRIMARY KEY,
    exchange    VARCHAR(64) NOT NULL,   -- e.g. "binance"
    symbol      VARCHAR(32) NOT NULL,   -- e.g. "BTC/USDT"
    timeframe   VARCHAR(8) NOT NULL,    -- e.g. "1h", "15m", "1d"
    ts          TIMESTAMPTZ NOT NULL,   -- candle open timestamp
    open        NUMERIC(24, 8) NOT NULL,
    high        NUMERIC(24, 8) NOT NULL,
    low         NUMERIC(24, 8) NOT NULL,
    close       NUMERIC(24, 8) NOT NULL,
    volume      NUMERIC(32, 8) NOT NULL,
    UNIQUE (exchange, symbol, timeframe, ts)
);
```

#### `strategy_versions`
```sql
CREATE TABLE strategy_versions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    strategy_id     UUID NOT NULL REFERENCES strategies(id) ON DELETE CASCADE,
    version         INTEGER NOT NULL,
    script          TEXT NOT NULL,               -- snapshot of the script at this version
    parameters      JSONB DEFAULT '{}',
    created_at      TIMESTAMPTZ DEFAULT now(),
    UNIQUE (strategy_id, version)
);
```

#### `notifications`
```sql
CREATE TABLE notifications (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type        VARCHAR(32) NOT NULL,           -- circuit_breaker | order_fill | auth_event | system
    title       VARCHAR(256) NOT NULL,
    body        TEXT,
    is_read     BOOLEAN DEFAULT FALSE,
    metadata    JSONB DEFAULT '{}',
    created_at  TIMESTAMPTZ DEFAULT now()
);
```

### 15.3 Migration Strategy (EF Core)

- **Autogeneration:** `dotnet ef migrations add DescriptiveName` from SQLAlchemy models equivalent (C# DbContext).
- **Applying DB Updates:** Database startup task (`dbContext.Database.MigrateAsync()`) applies pending migrations on container boot.
- **Rollback:** Every migration is reversible via `dotnet ef database update PreviousMigrationName`.
- **Zero-downtime on free-tier:** Since free-tier services cannot do blue/green deployments, all migrations must be **backwards-compatible** (add columns with defaults, never rename/drop until the next release).

---

## 16. User Interface Screens

### A. Broker Configuration

**Purpose:** Connect the application to real external exchanges.

**Features:**
- Form to add new broker (Dropdown: Binance / KuCoin / Coinbase). Includes fields for API Key, Secret (encrypted before storage), Fee %, and Sandbox toggle.
- "Test Connection" button calling the `/test` endpoint, which performs a dry-run balance or markets fetch to validate credentials instantly.
- List view of configured brokers with active/error status indicators.

### B. Strategy Editor

**Purpose:** The core IDE for writing, configuring, and testing the trading logic.

**Features:**
- **Code Editor:** Monaco Editor integration providing syntax highlighting for the Python DSL.
- **Configuration Panel:** Select Broker (from configured list), Trading Pair (e.g., BTC/USDT), Timeframe (1m, 5m, 1h, 1d), Tick interval (cron definition), and user-defined script parameters.
- **Circuit Breaker Status:** Visual indicator if the strategy is currently paused due to errors.
- **"Run Backtest" button:** Validates the script and triggers a historical test run without saving the strategy as active.
- **Historical Chart:** Candlestick chart displaying the backtest period with plot markers for the BUY/SELL signals generated by the script.

### C. Paper Trading

**Purpose:** Test strategies against live prices with no financial risk.

**Features:**
- Prominent banner: "Paper Trading — Simulated funds only. No real orders are placed."
- Virtual portfolio panel: configurable starting balance, current simulated holdings, unrealised P&L.
- Active paper strategies table: same columns as Live Strategies Dashboard (Status, Last Signal, P&L).
- Paper order history: all simulated fills with real market prices at execution time.
- Performance metrics: Win Rate, Max Drawdown, Sharpe Ratio — calculated against live price data.
- Side-by-side comparison: Paper P&L vs. Backtest P&L for the same strategy and period.
- Promote to Live button: moves a strategy from Paper to Live mode after a confirmation dialog.

### D. Live Strategies Dashboard

**Purpose:** Monitor and control active live trading bots.

**Features:**
- Data table of all active strategies: Status, Mode, Last Signal, current P&L.
- Circuit breaker state badge per strategy (Closed / Open / Half-Open).
- Controls: Pause, Resume, Terminate (with confirmation).
- Real-time P&L updates via authenticated SignalR WebSocket.
- Error log panel: last N errors for each strategy, with timestamps and reasons.

### E. Order History

**Purpose:** Full audit trail of all trading activity.

**Features:**
- Unified order table filterable by Mode (live / paper / backtest), Status, Symbol, Date range.
- Columns: Timestamp, Strategy, Symbol, Side, Quantity, Fill Price, Fees, Status, Broker Confirmation ID.
- CSV export.

### F. Settings

**Purpose:** Account and session management.

**Features:**
- Password change (triggers Argon2id re-hash and full session revocation).
- Active sessions list with device/IP info and individual force-logout buttons.
- Notification preferences: email alerts for circuit breaker trips, order fills, and authentication events.

---

## 17. Deployment

### 17.1 Docker Compose Services

Because Hangfire Server runs inside the .NET Worker process, we do not need a separate `beat` container like the Python Celery architecture did.

```yaml
services:
  frontend:
    build: ./frontend
    ports: ["3000:3000"]

  api:
    build:
      context: ./backend
      dockerfile: api.Dockerfile        # Standard ASP.NET Core image
    ports: ["8000:8000"]
    environment:
      - DefaultConnection
      - REDIS_URL
      - MASTER_ENCRYPTION_KEY
      - JWT_SECRET
      - ALLOWED_ORIGINS
    # NOTE: API does NOT mount docker.sock — only the worker spawns containers

  worker:
    build:
      context: ./backend
      dockerfile: worker.Dockerfile     # Includes Docker CLI for spawning python strategy containers
    environment:
      - DefaultConnection
      - REDIS_URL
      - MASTER_ENCRYPTION_KEY
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock  # Required for strategy container spawning

  db:
    image: postgres:16-alpine
    environment:
      - POSTGRES_DB=trading
      - POSTGRES_USER
      - POSTGRES_PASSWORD
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
    volumes:
      - redisdata:/data

volumes:
  pgdata:
  redisdata:
```

### 17.2 Health Check Endpoints

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

// Configure EF Core and Redis health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration["REDIS_URL"]!);
```

### 17.3 Free-Tier Deployment Matrix

| Service | Platform | Notes |
|---|---|---|
| API (ASP.NET) | Google Cloud Run | Fast start time with AOT/Trimmed build |
| Worker (Hangfire) | Oracle Cloud Always Free | 4 ARM OCPUs, always-on |
| Database | Neon (PostgreSQL) | Generous free tier, serverless |
| Redis | Upstash Redis | Free tier: 10,000 commands/day, persistent |
| Frontend | Vercel | Free tier, global CDN |

### 17.4 Environment Variables

All secrets are managed as environment variables. They are never committed to source control.

```
DefaultConnection         PostgreSQL connection string
REDIS_URL                 Redis connection string (includes password)
REDIS_PASSWORD            Redis requirepass value
MASTER_ENCRYPTION_KEY     32-byte hex key for AES-256-GCM (generate with: openssl rand -hex 32)
JWT_SECRET                HS256 signing secret for JWTs
ALLOWED_ORIGINS           Comma-separated list of allowed frontend origins
```

---

## 18. API Contract

### 18.1 Authentication

| Method | Path | Request Body | Response | Notes |
|---|---|---|---|---|
| POST | `/api/auth/register` | `{ email, password }` | `201 { id, email }` | Rate: 3/min/IP |
| GET | `/api/auth/verify` | `?token=...` | `200 { verified: true }` | Email verification |
| POST | `/api/auth/login` | `{ email, password }` | `200 { access_token }` + Set-Cookie | Rate: 5/min/IP |
| POST | `/api/auth/refresh` | (cookie) | `200 { access_token }` | Rate: 20/min/IP |
| POST | `/api/auth/logout` | — | `204` | Revokes refresh token |

### 18.2 Brokers

| Method | Path | Request Body | Response | Notes |
|---|---|---|---|---|
| GET | `/api/brokers` | — | `200 [ { id, broker_name, status, fee_rate, is_sandbox } ]` | List user's brokers |
| POST | `/api/brokers` | `{ broker_name, api_key, secret, fee_rate, funding_rate, is_sandbox }` | `201 { id }` | Secret encrypted before DB |
| GET | `/api/brokers/{id}/test` | — | `200 { connected: true }` | Tests connection to exchange |
| DELETE | `/api/brokers/{id}` | — | `204` | Cascades to strategies |
| GET | `/api/brokers/{id}/markets` | — | `200 [ { symbol, base, quote } ]` | Cached 300s |
| GET | `/api/brokers/{id}/ticker/{symbol}` | — | `200 { last, bid, ask, volume }` | Cached 5s |

### 18.3 Strategies

| Method | Path | Request Body | Response | Notes |
|---|---|---|---|---|
| GET | `/api/strategies` | — | `200 [ { id, name, status, mode, symbol, pnl } ]` | |
| POST | `/api/strategies` | `{ name, credential_id, symbol, timeframe, script, parameters, mode, max_position_size, max_daily_volume }` | `201 { id }` | |
| PUT | `/api/strategies/{id}` | (partial update) | `200` | Script edit creates new version |
| POST | `/api/strategies/{id}/start` | — | `200` | Starts live/paper execution |
| POST | `/api/strategies/{id}/pause` | — | `200` | Pauses execution |
| POST | `/api/strategies/{id}/backtest` | `{ start_date, end_date }` | `202 { task_id }` | Async Hangfire task |
| GET | `/api/strategies/{id}/executions` | `?mode=&limit=` | `200 [ ... ]` | |

### 18.4 Orders

| Method | Path | Request Body | Response | Notes |
|---|---|---|---|---|
| GET | `/api/orders` | `?mode=&status=&symbol=&from=&to=&limit=` | `200 [ ... ]` | Filterable |
| GET | `/api/orders/export` | `?format=csv` | `200 text/csv` | CSV download |

### 18.5 Notifications & Settings

| Method | Path | Request Body | Response | Notes |
|---|---|---|---|---|
| GET | `/api/notifications` | `?unread=true&limit=` | `200 [ ... ]` | |
| PUT | `/api/notifications/{id}/read` | — | `200` | |
| PUT | `/api/settings/password` | `{ current, new_password }` | `200` | Re-hash + revoke sessions |
| GET | `/api/settings/sessions` | — | `200 [ { id, ip, user_agent, created_at } ]` | |
| DELETE | `/api/settings/sessions/{id}` | — | `204` | Force-logout |

### 18.6 Error Response Format

All errors follow the **RFC 7807 Problem Details** standard built into ASP.NET Core:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-84c1a4b...01",
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
```

---

## 19. WebSocket Protocol (SignalR)

### 19.1 Connection

The application uses **ASP.NET Core SignalR** instead of raw WebSockets, providing automatic reconnection, robust payload serialization, and built-in JWT authentication.

```
wss://api.example.com/hubs/trading?access_token={access_token}
```

### 19.2 Hub Events (Server-to-Client)

Instead of manually parsing discriminated JSON unions, the frontend subscribes to strongly-typed events:

- `OnStrategyUpdate(strategyId, status, lastSignal, pnl)`
- `OnOrderFill(orderId, symbol, side, fillPrice)`
- `OnNotification(notificationId, title, type)`
- `OnBacktestProgress(taskId, percent, currentDate)`
- `OnBacktestComplete(taskId, pnl, trades, sharpe)`

### 19.3 Reconnection & Resiliency

SignalR automatically manages exponential backoff reconnection. If a client disconnects, they are responsible for re-fetching aggregate state via REST upon successful reconnection to reconcile missed events.

---

## 20. Notification System

### 20.1 Architecture

Notifications are delivered through two channels:

1. **In-app (real-time):** Stored in the `notifications` EF Core entity and pushed to the connected user immediately via the SignalR `TradingHub`.
2. **Email (deferred):** Dispatched via a generic Hangfire background job (`NotifyUserJob`) using an SMTP client like `MailKit`.

### 20.2 Trigger Events

| Event | In-App (SignalR) | Email (Hangfire) | Priority |
|---|---|---|---|
| Circuit breaker tripped | ✓ | ✓ | High |
| Order filled (live only) | ✓ | Optional | Normal |
| Strategy error | ✓ | ✓ | High |
| Login from new IP | ✓ | ✓ | High |
| Password changed | ✓ | ✓ | High |
| Backtest completed | ✓ | — | Normal |

### 20.3 User Preferences

Users configure notification preferences in the Settings page. Preferences are mapped to a JSON column (`jsonb` in PostgreSQL) on the `users` table:

```json
{
  "email_circuit_breaker": true,
  "email_order_fill": false,
  "email_auth_events": true
}
```
