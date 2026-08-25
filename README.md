# Conference Rooms API

Repository: [github.com/jfoere/abp-task](https://github.com/jfoere/abp-task)

An ASP.NET Core Web API for managing conference rooms, searching availability, creating bookings with time-based pricing, and viewing business reports.

The original assignment is available in [task.md](task.md), and the agreed implementation decisions are recorded in [PLAN.md](PLAN.md).

## Features

- Public room catalog and availability search.
- Admin-only room creation, updates, and soft deletion.
- Customer and Admin booking creation.
- Segmented morning, standard, peak, and evening pricing.
- One-time optional-service charges and immutable booking price snapshots.
- Revenue, room-utilization, and service-usage reports.
- Public Swagger UI with API-key authorization support.
- Per-IP and per-key rate limiting.
- Automatic EF Core migrations and idempotent initial-data seeding.

## Technology

- .NET 8 and ASP.NET Core controllers
- EF Core 8
- SQLite
- Swagger/OpenAPI through Swashbuckle
- xUnit integration and unit tests

## Architecture

```text
src/
  ConferenceRooms.Api/             The single production project
    Business/                      Business rules, use cases, contracts
    Data/                          EF Core, SQLite, migrations, repositories
    Controllers/                   HTTP endpoints
    Auth/                          API-key authentication and authorization
tests/
  ConferenceRooms.Tests/           Unit and integration tests
```

The production code uses one project to keep this small assignment easy to open, run, and explain. Folders still separate responsibilities: `Business` contains business behavior and repository contracts, `Data` implements database access, and the controllers turn HTTP requests into business operations. These are organizational boundaries rather than separate compiled assemblies.

EF Core maps the domain entities to SQLite tables. Database queries stay in repositories, and `Database.MigrateAsync()` applies pending schema migrations without clearing existing data.

## Business Rules

- Times supplied to the API must include an offset matching `Europe/Kyiv`.
- Times are stored in UTC and converted to business-local time for pricing.
- Bookings use full-hour slots, last at least one hour, and stay within `06:00–23:00` on one day.
- Back-to-back bookings are valid; only true overlaps are rejected.
- Services are charged once per booking.
- Deleted rooms behave as missing, but their historical bookings remain.

| Local time | Room-rate rule |
|---|---|
| 06:00–09:00 | 10% discount |
| 09:00–12:00 | Standard rate |
| 12:00–14:00 | 15% peak surcharge |
| 14:00–18:00 | Standard rate |
| 18:00–23:00 | 20% discount |

A booking from 11:00–15:00 in Room A costs UAH 8,600 for the room: one standard hour, two peak hours, and one standard hour. Projector and Wi-Fi add UAH 800 once, producing a UAH 9,400 total.

## Initial Data

All seeded rooms support all three seeded services.

| Room | ID | Capacity | Hourly rate |
|---|---|---:|---:|
| Room A | `20000000-0000-0000-0000-000000000001` | 50 | UAH 2,000 |
| Room B | `20000000-0000-0000-0000-000000000002` | 100 | UAH 3,500 |
| Room C | `20000000-0000-0000-0000-000000000003` | 30 | UAH 1,500 |

| Service | ID | Price per booking |
|---|---|---:|
| Projector | `10000000-0000-0000-0000-000000000001` | UAH 500 |
| Wi-Fi | `10000000-0000-0000-0000-000000000002` | UAH 300 |
| Sound | `10000000-0000-0000-0000-000000000003` | UAH 700 |

## Run Locally

### Prerequisites

- .NET 8 SDK
- PowerShell for the example setup commands

Restore the repository-local EF migration tool and NuGet packages:

```powershell
dotnet tool restore
dotnet restore
```

Start the API:

```powershell
dotnet run --project .\src\ConferenceRooms.Api\ConferenceRooms.Api.csproj --launch-profile https
```

Open [https://localhost:7100/swagger](https://localhost:7100/swagger). The **Authorize** dialog displays two Admin and two Customer credentials that are ready for local testing.

Swagger provides valid example values for every operation. The examples use seeded IDs and matching Kyiv timestamps. After creating Room D, copy its returned ID into the update or delete example; after creating a booking, change its date or time before running that example again.

The local SQLite database is created at `src/ConferenceRooms.Api/App_Data/conference-rooms.db`. `App_Data` and database files are ignored by Git.
The fixed Development credentials and SQLite setup are intended for local/demo use only.

## API

| Method | Endpoint | Access |
|---|---|---|
| GET | `/api/rooms` | Public |
| GET | `/api/rooms/{id}` | Public |
| GET | `/api/rooms/available` | Public |
| POST | `/api/rooms` | Admin |
| PUT | `/api/rooms/{id}` | Admin |
| DELETE | `/api/rooms/{id}` | Admin |
| POST | `/api/bookings` | Customer or Admin |
| GET | `/api/reports/revenue` | Admin |
| GET | `/api/reports/utilization` | Admin |
| GET | `/api/reports/services` | Admin |

Protected requests supply the key in this header:

```http
X-API-Key: <key-value>
```

See [ConferenceRooms.Api.http](src/ConferenceRooms.Api/ConferenceRooms.Api.http) for sample requests.

## Errors and Security

- Validation, missing resources, conflicts, and unexpected errors use RFC 7807 Problem Details.
- API keys are unique, at least 32 characters long, and compared using SHA-256 hashes in constant time.
- Production requires separately configured Admin and Customer keys; only local Development credentials appear in Swagger.
- Rate limits are 60 requests per minute for public endpoints, 30 per authenticated key, and 10 per IP for failed authentication attempts.
- Exceeded limits return `429 Too Many Requests` with `Retry-After: 60`.

## Database Migrations

Create a migration after changing the EF Core model:

```powershell
dotnet tool run dotnet-ef migrations add <MigrationName> `
  --project .\src\ConferenceRooms.Api\ConferenceRooms.Api.csproj `
  --output-dir Data\Persistence\Migrations `
  --namespace ConferenceRooms.Data.Persistence.Migrations
```

Migrations run automatically at application startup. Seed logic inserts only missing reference records, so restarting the application does not duplicate or erase data.

## Tests

Run the complete suite after restoring packages:

```powershell
dotnet test
```

The integration tests use isolated temporary SQLite databases rather than the EF Core in-memory provider, so SQLite mappings and queries are exercised.
