# Project Architecture - Darkhorse

This document describes the technical architecture and design decisions of the Darkhorse trading platform, built with .NET 9.

## 🏛 Clean Architecture Layers

### 1. Domain Layer
Contains the core business logic and entities. It has zero dependencies on external frameworks or infrastructure.
- **Entities:** `User`, `Strategy`, `BrokerCredential`, `Order`, `Execution`, `StrategyVersion`, `DataHistory`, `Notification`, `AuditLog`.
- **Interfaces:** Repository and Service abstractions (e.g., `IBrokerService`, `IStrategyRunner`).
- **Exceptions:** Domain-specific business exceptions like `CircuitOpenException` or `StrategyExecutionException`.

### 2. Application Layer
Implements the **CQRS (Command Query Responsibility Segregation)** pattern using **MediatR**.
- **Commands:** Mutate state (e.g., `RegisterCommand`, `StartStrategyCommand`, `CreateBrokerCommand`).
- **Queries:** Read-only data access (e.g., `GetBrokersQuery`, `GetOrdersQuery`).
- **Validation:** FluentValidation rules for strictly typed input sanitization.

### 3. Infrastructure Layer
Handles persistence, security, and external integrations.
- **EF Core:** PostgreSQL implementation for all repositories with high-precision decimals (`24,8`).
- **Security:** 
  - **Argon2id:** Password hashing using `Konscious.Security.Cryptography.Argon2`.
  - **AES-256-GCM:** Encrypting exchange API keys with unique 96-bit nonces.
- **Resilience:** **Polly** circuit breakers to manage broker API failures.
- **Caching:** Redis implementation for ticker data and session management.

### 4. Web API Layer
The entry point for frontend and mobile clients.
- **Authentication:** JWT Bearer tokens with custom lifespan (15 min).
- **Security Middleware:** 
  - **CSRF:** Double-submit cookie pattern.
  - **Rate Limiting:** `AspNetCoreRateLimit` for DDoS protection.
- **SignalR:** Real-time push notifications for order fills, P&L updates, and backtest progress.

### 5. Worker Layer
A dedicated background processor using **Hangfire**.
- **Strategy Scheduler:** Polls active strategies every minute.
- **Data Ingestion:** Daily OHLCV extraction from exchanges to the local PostgreSQL historical store.
- **Sandbox Manager:** Orchestrates the lifecycle of ephemeral Docker containers for strategy execution.

---

## 🛡 Strategy Execution Isolation

To prevent malicious or buggy user scripts from compromising the server, strategies are executed using a **Sidecar Sandboxing** approach:

1. **Isolation:** Each strategy execution spawns an ephemeral Docker container (`strategy-runner:latest`).
2. **Resource Limits:** Containers are capped on CPU and Memory usage via Docker engine flags.
3. **No Inbound Networking:** Data is provided to the script via **stdin** in JSON format.
4. **Communication:** Results (signals/logs) are returned via **stdout** as JSON and parsed back into the .NET Application layer.

---

## 🔄 Data Flows

### Real-Time Update Flow
1. **ExchangeSharp** fetches a ticker or an order is filled on the exchange.
2. The **Worker** persists the change in PostgreSQL and updates the audit log.
3. The **Worker** publishes a message to the **SignalR Hub**.
4. The **Frontend** receives the event via WebSocket and updates the dashboard instantly.

### Strategy Execution Flow
1. **Hangfire** triggers the `TickStrategiesJob`.
2. The system retrieves the strategy script, current OHLCV slice, and account balance.
3. A JSON payload is piped into a `strategy-runner` container.
4. The Python script returns a signal (BUY/SELL/HOLD).
5. If an order is signaled, the **BrokerService** executes it and updates the circuit breaker state.

---

## 📊 Persistence Strategy

- **PostgreSQL:** Primary store for relational data and high-volume historical OHLCV data.
- **JSONB:** Used for strategy parameters and version snapshots to allow flexibility in strategy logic.
- **Redis:** Used for distributed locks, ticker caching, and short-lived resilience state.
- **Security:** Sensitive database columns (Broker Secrets) are encrypted at the application level before insertion.
