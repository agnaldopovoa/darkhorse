# Darkhorse -- Technical Architecture Documentation

## 1. System Overview

Darkhorse is a trading strategy execution platform composed of: -
Frontend (React + Vite) - Backend API (.NET) - Worker (.NET Background
Service) - Python Strategy Runner

## 2. Architecture

Frontend → API → Application → Infrastructure → Database\
Worker → Strategy Executor → Broker / Python

## 3. Backend

Clean Architecture: - Domain - Application - Infrastructure - API -
Worker

## 4. Key Patterns

-   Clean Architecture
-   Partial CQRS
-   Repository Pattern (EF Core)
-   Circuit Breaker
-   Caching (Redis)

## 5. Security

-   Password hashing
-   Credential encryption
-   CSRF protection
-   Likely JWT

## 6. Observations

Strengths: - Good separation of concerns - Real-time communication
(SignalR) - Background processing

Improvements: - CQRS incomplete - Overuse of repositories - Missing
validation layer - Worker coupling

## 7. Conclusion

The system is well-structured and close to production-grade
architecture.
