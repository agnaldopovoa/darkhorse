# Implementation Plan — Darkhorse Trading Platform (.NET)

Based on the [architecture_net.md](file:///d:/Projects/darkhorse/architecture_net.md) document.

## Phase 1 — Project Setup

### Top-Level
- [NEW] `docker-compose.yml` — PostgreSQL 16, Redis 7 (with `requirepass`), API, worker, frontend
- [NEW] `.env.example` — Template for all environment variables (§16.4)
- [NEW] `.gitignore` — Ignore `bin/`, `obj/`, node_modules, `.env`, etc.

### Backend (.NET 8 Solution)
- [NEW] `Darkhorse.sln`
- [NEW] `Darkhorse.Domain/Darkhorse.Domain.csproj` — Core entities and interfaces
- [NEW] `Darkhorse.Application/Darkhorse.Application.csproj` — Use Cases, CQRS, DTOs
- [NEW] `Darkhorse.Infrastructure/Darkhorse.Infrastructure.csproj` — EF Core, Hangfire, ExchangeSharp
- [NEW] `Darkhorse.Api/Darkhorse.Api.csproj` — API Controllers, SignalR (Presentation)
- [NEW] `Darkhorse.Worker/Darkhorse.Worker.csproj` — Hangfire Worker Project
- [NEW] `api.Dockerfile` — ASP.NET Core image
- [NEW] `worker.Dockerfile` — Worker image containing Docker CLI to spawn Python runners

### Frontend
- [NEW] `frontend/` — Vite + React + TypeScript scaffold

## Phase 2 — Domain & Storage (Infrastructure)

- [NEW] `Darkhorse.Domain/Entities/*.cs` — Entities (`User`, `BrokerCredential`, `Strategy`, `StrategyVersion`, `Order`, `Execution`, `AuditLog`, `Notification`, `DataHistory`)
- [NEW] `Darkhorse.Domain/Interfaces/Repositories/*.cs` — `IUserRepository`, `IStrategyRepository` etc.
- [NEW] `Darkhorse.Infrastructure/Data/AppDbContext.cs` — EF Core DbContext
- [NEW] `Darkhorse.Infrastructure/Repositories/*.cs` — EF Core repository implementations
- [NEW] EF Core Migrations setup (`dotnet ef migrations add Initial`)

### Verification
- `tests/Darkhorse.Domain.Tests/EntityTests.cs`
- Apply migration against local PostgreSQL, inspect schema in pgAdmin

## Phase 3 — Authentication & Security

- [NEW] `Darkhorse.Infrastructure/Security/PasswordService.cs` — Argon2id hashing
- [NEW] `Darkhorse.Infrastructure/Security/CredentialEncryption.cs` — AES-256-GCM encryption/decryption
- [NEW] `Darkhorse.Application/Auth/Commands/*.cs` — `RegisterCommand`, `LoginCommand`, `LogoutCommand`
- [NEW] `Darkhorse.Api/Controllers/AuthController.cs` — Presentation layer routing to MediatR
- [NEW] `Darkhorse.Api/Middleware/CsrfMiddleware.cs` — Double-submit cookie pattern
- [NEW] Serilog configuration with `CorrelationIdMiddleware`

### Verification
- `tests/Darkhorse.Application.Tests/AuthCommandTests.cs` — Registration, login, JWT flow

## Phase 4 — Broker Integration

- [NEW] `Darkhorse.Application/Interfaces/IBrokerService.cs` — Broker boundary
- [NEW] `Darkhorse.Infrastructure/Services/BrokerAdapter.cs` — ExchangeSharp wrapper (Binance, KuCoin, Coinbase)
- [NEW] `Darkhorse.Application/Brokers/Queries/*.cs` — Get Markets, Get Tickers
- [NEW] `Darkhorse.Api/Controllers/BrokersController.cs` — Presentation routes

## Phase 5 — Strategy Engine

- [NEW] `strategy-runner/Dockerfile` — Minimal Python image with `runner.py` baked in
- [NEW] `strategy-runner/runner.py` — Fixed Python harness with restricted `__builtins__`
- [NEW] `Darkhorse.Application/Interfaces/IStrategyRunner.cs`
- [NEW] `Darkhorse.Infrastructure/Services/StrategyExecutor.cs` — Spawns Docker CLI Process
- [NEW] `Darkhorse.Infrastructure/Resilience/CircuitBreakerFactory.cs` — Polly Circuit Breaker
- [NEW] `Darkhorse.Api/Controllers/StrategiesController.cs` — Presentation routes

## Phase 6 — Backtesting & Hangfire

- [NEW] `Darkhorse.Worker/Program.cs` — Hangfire Server configuration with PostgreSQL storage
- [NEW] `Darkhorse.Worker/Jobs/RunBacktestJob.cs` — Hangfire background job for backesting
- [NEW] `Darkhorse.Worker/Jobs/ExecuteStrategyJob.cs` — Hangfire cron job for live strategy ticks
- Single long-lived Python container approach for backtesting over historical DB data

## Phase 7 — Frontend (React)

Build all UI screens defined in §15:
- Login / Register page
- Broker Configuration (cards + form)
- Strategy Editor (Monaco Editor + config panel)
- Paper Trading dashboard
- Live Strategies dashboard (SignalR WebSocket-powered)
- Order History (filterable table + CSV export)
- Settings (password, sessions, notifications)

## Phase 8 — Deployment

- Configure `worker.Dockerfile` with Hangfire
- Set up CI/CD for Docker builds
- Deploy to target free-tier platforms (§16.3)
- Configure HTTPS/TLS at edge

## Verification Plan

### Automated
1. `docker-compose up -d db redis` — verify services start
2. `dotnet ef database update` — verify migrations apply cleanly
3. `dotnet test` — security, auth, broker, strategy unit tests
4. Boot API layer, verify `/health` and Swagger UI

### Manual
1. Inspect encrypted `apikey_cipher` and `secret_cipher` columns in pgAdmin
2. Test SignalR WebSocket connection with JWT token
3. Run a backtest job in the Hangfire Dashboard, verify Python container execution and output parsing
