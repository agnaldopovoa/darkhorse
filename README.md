# Darkhorse Trading Platform

Darkhorse is a high-performance, secure, and extensible cryptocurrency trading platform designed for executing custom strategies with institutional-grade isolation.

## 🚀 Overview

The platform allows users to develop, backtest, and deploy trading strategies written in a Python DSL. It leverages **Clean Architecture** with .NET 9 and focuses on security, resilience, and real-time execution.

## 🛠 Tech Stack

- **Backend:** .NET 9 (C#)
- **Frontend:** React + TypeScript + Vite + Tailwind CSS
- **Persistence:** PostgreSQL 16 (Relational), Redis 7 (Caching & Resilience)
- **Background Processing:** Hangfire (PostgreSQL storage)
- **Isolation:** Docker (Ephemeral containers for strategy execution)
- **Messaging:** SignalR (Real-time dashboard updates)
- **Integration:** ExchangeSharp (Broker connectivity)

## 🏗 System Architecture

The project follows the **Clean Architecture** pattern:

- **Domain:** Pure business logic, entities, and interfaces.
- **Application:** CQRS pattern (MediatR), commands, queries, and validation.
- **Infrastructure:** EF Core, Security services (AES-256-GCM, Argon2id), Redis, and External API adapters.
- **Web API:** RESTful endpoints, JWT Auth, CSRF protection, and SignalR Hub.
- **Worker:** Background job processor for strategy polling, OHLCV ingestion, and notifications.
- **Strategy Runner:** Sandboxed Python environment for execution.

For a deep dive into the technical details, see [architecture.md](./architecture.md).

## 🔒 Security Features

- **Passwords:** Argon2id hashing.
- **Broker Keys:** AES-256-GCM encryption with unique nonces per key.
- **Web Security:** JWT with CSRF double-submit cookie patterns.
- **Sandboxing:** Strategies run in non-root Docker containers with resource limits.

## 🚦 Getting Started

### Prerequisites

- Docker Desktop / Docker Compose
- .NET 9 SDK (for local development)
- Node.js & npm (for frontend development)

### Quick Start (Docker)

To launch the entire stack (Database, Cache, API, Worker, and Frontend):

```bash
docker compose up --build -d
```

- **Frontend:** `http://localhost:3000`
- **API Health:** `http://localhost:5000/health`
- **Swagger UI:** `http://localhost:5000/swagger`
- **Hangfire Dashboard:** `http://localhost:5001/hangfire` (accessible via Worker port)

## 🐳 Project Structure

```text
├── backend/
│   ├── Api/            # Web API & SignalR
│   ├── Application/    # CQRS Handlers
│   ├── Domain/         # Entities & Interfaces
│   ├── Infrastructure/ # DB & External Services
│   └── Worker/         # Background Jobs
├── frontend/           # Vite + React App
├── strategy-runner/    # Python isolated runner
└── Darkhorse.sln       # Main C# Solution
```

## 📜 License

This project is proprietary. All rights reserved.
