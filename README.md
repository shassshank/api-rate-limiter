# ASP.NET Core Rate Limiting Middleware

A production-grade, extensible rate limiting system built on ASP.NET Core.  
It implements Fixed Window, Sliding Window, and Token Bucket algorithms with identity-aware throttling using API keys, user claims, and IP addresses.  
The system is backed by Redis for distributed enforcement and structured with clean, modern engineering practices.

This project is intentionally built to resemble real infrastructure you'd place behind an API gateway.

---

## Tech Stack

- **Backend Framework:** ASP.NET Core (.NET 8), C#
- **Architecture & Patterns:** Custom middleware, Strategy Pattern, Options Pattern, dependency injection
- **Distributed Caching:** Redis via `IDistributedCache` (with in-memory fallback for development/testing)
- **Testing Frameworks:** xUnit, in-memory cache test doubles, isolated strategy and selector tests
- **Tooling & Observability:** .NET CLI, logging abstractions, Swagger/OpenAPI
---

## Key Features

### Multiple Rate Limiting Algorithms
All strategies implement a shared interface and are registered through dependency injection:

- **Fixed Window** - predictable, window-based throttling  
- **Sliding Window** - rolling window for smoother traffic control  
- **Token Bucket** - burst-friendly, refill-based limiting  

Each strategy is isolated, testable, and easily extendable.

---

## Identity-Aware Throttling
Every incoming request is mapped to a rate-limit identity:

1. **API Key** (`X-Api-Key`)
2. **Authenticated User** (JWT-ready)
3. **IP Address**

API keys include name and plan metadata:

```json
"ApiKeys": {
  "Keys": [
    { "Key": "FREE-111", "Name": "Free Client", "Plan": "Free" },
    { "Key": "PRO-777",  "Name": "Pro Client",  "Plan": "Pro" }
  ]
}
```
This enables different clients and tenants to have different rate-limit behaviors.

---

## Policy-Based Strategy Selection
Different endpoints can be mapped to different rate-limiting strategies - similar to what NGINX, Envoy, and Cloudflare expose.

```json
"RateLimitPolicies": {
  "Rules": [
    { "PathPrefix": "/api/demo/fixed",   "Strategy": "FixedWindow" },
    { "PathPrefix": "/api/demo/sliding", "Strategy": "SlidingWindow" },
    { "PathPrefix": "/api/demo/token",   "Strategy": "TokenBucket" }
  ]
}
```
This lets the API choose between strict, burst-friendly, or smoothed rate-limit behavior on a per-route basis.

---

## Redis Backing (Distributed)

The rate limiter uses `IDistributedCache` (Redis) for its counters and timestamp buckets.
This makes it horizontally scalable - multiple application instances share the same state.

For local development, it seamlessly falls back to an in-memory cache.

---

## Project Structure
```
src/
  API/
    Controllers/
    Middleware/
    RateLimiting/
    Configuration/
    Models/
    Program.cs
    appsettings.json

tests/
  API.RateLimiting.Tests/
    TestHelpers/
    *.cs files
    README.md
```

---

### Folder Responsibilities

- `RateLimiting/` - all algorithms, identity resolution, strategy selector
- `Middleware/` - pipeline, headers, Retry-After formatting, JSON errors
- `Models/` - API DTOs (e.g., `RateLimitErrorResponse`)
- `Configuration/` - strongly typed option bindings
- `Tests/` - isolated tests covering strategies and selectors

---

## Running the API
```bash
cd src/API
dotnet run
```

Swagger is available at: 
```
/swagger 
```

Example request:
```bash
curl -H "X-Api-Key: FREE-111" https://localhost:<port>/api/demo/token
```
Replace `<port>` with the port shown in your terminal after running `dotnet run`.

---

## Test Suite

### Unit tests are located under: 
`tests/API.RateLimiting.Tests`

Coverage includes:
- Fixed Window behavior
- Sliding Window timestamp logic
- Token Bucket token consumption/refill
- Strategy selection (path-based policies)
- Identity resolution

### Run all tests:

```bash
dotnet test tests/API.RateLimiting.Tests
```

All tests run in memory using MemoryDistributedCache — no external Redis needed.

---

## Purpose

This is a ground-up implementation of distributed rate limiting — not a wrapper around .NET’s built-in limiter.
It’s designed to demonstrate:
- Practical system design
- Middleware architecture
- Clean abstractions
- Distributed caching patterns
- Algorithm correctness
- Testability and maintainability
- Configuration-driven behavior

A compact yet realistic backend architecture appropriate for production scenarios, and a solid demonstration of practical backend engineering depth.

---
