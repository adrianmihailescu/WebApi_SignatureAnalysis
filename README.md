# Security Detection Platform

Ready-to-polish MVP for the Senior Software Engineer technical assignment.

## Stack
- .NET 10 ASP.NET Core Web API
- EF Core 10 + SQL Server 2022
- JWT authentication + roles
- SignalR real-time updates
- React + TypeScript + Vite

## Run

1. Start SQL Server:
```bash
docker compose up -d
```

2. Run API:
```bash
cd backend/SecurityDetection.Api
dotnet restore
dotnet run
```

3. Run frontend:
```bash
cd frontend/security-detection-ui
npm install
npm run dev
```

If the API HTTPS port is not `7001`, change `src/config.ts` in the frontend.

## Demo users
- `admin` / `Admin123!` -> Admin
- `support` / `Support123!` -> ReadOnly
- `analyst` / `Analyst123!` -> SecurityAnalyst

## Demo incident
Use Swagger:
`POST /api/incidents`
```json
{
  "customerId": 1,
  "payload": {
    "eventType": "malware",
    "severity": "critical",
    "hostname": "SERVER-01"
  }
}
```

Acme has importance 10 and Critical Malware has priority 10, so the detection priority is 20.

## Important design decisions
- All persisted timestamps are UTC (`CreatedAtUtc`, `ClaimedAtUtc`, `ResolvedAtUtc`).
- Signature conditions are JSON for a compact dynamic schema.
- All signature conditions must match. Multiple matches use highest signature priority, then lowest ID.
- Unmatched incidents return `204 No Content` and are ignored in this MVP.
- Queue is separate from history: queue contains only `Open` detections.
- Claim uses an atomic conditional `UPDATE`; a concurrent loser receives `409 Conflict`.
- A SQL Server filtered unique index ensures one `Assigned` detection per analyst.
- SignalR is used only for UI synchronization; SQL Server remains the source of truth.
- This is intentionally a modular monolith for the three-hour assignment. It can later be horizontally scaled behind a load balancer, use Azure SignalR for multi-instance fan-out, and move high-volume ingestion behind a message broker.

## Production polish ideas
Replace `EnsureCreated()` with EF Core migrations, move JWT/database secrets to environment variables or a secret store, add structured logging, DTO validation, paging, audit history, and integration tests.
