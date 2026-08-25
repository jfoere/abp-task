# Conference Rooms API

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

### EF Core in this project

EF Core maps the C# domain entities to SQLite tables:

- `ConferenceRoomsDbContext` is the database session.
- Entity configurations define tables, keys, relationships, indexes, and the active-room query filter.
- A migration describes how to create or update the schema.
- Repositories contain database queries so business services do not depend on EF Core.
- `Database.MigrateAsync()` applies only pending migrations; it does not recreate or clear an existing database.

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

Generate four random API keys:

```powershell
function New-ConferenceRoomsApiKey {
    $keyBytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    $generator.GetBytes($keyBytes)
    $generator.Dispose()
    [Convert]::ToBase64String($keyBytes)
}

$adminKey1 = New-ConferenceRoomsApiKey
$adminKey2 = New-ConferenceRoomsApiKey
$customerKey1 = New-ConferenceRoomsApiKey
$customerKey2 = New-ConferenceRoomsApiKey
```

Store them outside the repository with .NET user secrets:

```powershell
$apiProject = ".\src\ConferenceRooms.Api\ConferenceRooms.Api.csproj"
dotnet user-secrets set --project $apiProject "ApiKeys:Clients:0:Key" $adminKey1
dotnet user-secrets set --project $apiProject "ApiKeys:Clients:1:Key" $adminKey2
dotnet user-secrets set --project $apiProject "ApiKeys:Clients:2:Key" $customerKey1
dotnet user-secrets set --project $apiProject "ApiKeys:Clients:3:Key" $customerKey2
```

Start the API:

```powershell
dotnet run --project .\src\ConferenceRooms.Api\ConferenceRooms.Api.csproj --launch-profile https
```

Open [https://localhost:7100/swagger](https://localhost:7100/swagger). Use Swagger's **Authorize** button and enter one of the generated key values.

The local SQLite database is created at `src/ConferenceRooms.Api/App_Data/conference-rooms.db`. `App_Data` and database files are ignored by Git.

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
- API keys are SHA-256 hashed before constant-time comparison.
- Configured keys must be unique and at least 32 characters long.
- Production startup requires at least one Admin key and one Customer key.
- Public endpoints allow 60 requests per minute per IP.
- Protected endpoints allow 30 requests per minute per authenticated key.
- Missing or invalid keys on protected endpoints allow 10 attempts per minute per IP.
- Exceeded limits return `429 Too Many Requests` with `Retry-After: 60`.
- Key values are never included in source control or Swagger.

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

## Manual Azure App Service Deployment

This project intentionally uses a demo-only SQLite deployment. Create a Windows Azure App Service configured for code deployment with the .NET 8 runtime, keep it at one instance, and do not enable autoscaling.

In the App Service **Environment variables** page, set four secret values:

```text
ApiKeys__Clients__0__Key = <admin-1-key>
ApiKeys__Clients__1__Key = <admin-2-key>
ApiKeys__Clients__2__Key = <customer-1-key>
ApiKeys__Clients__3__Key = <customer-2-key>
```

Publish and create a ZIP from the contents of the publish directory:

```powershell
dotnet publish .\src\ConferenceRooms.Api\ConferenceRooms.Api.csproj `
  --configuration Release `
  --output .\publish

Compress-Archive -Path .\publish\* -DestinationPath .\conference-rooms.zip
```

Deploy the ZIP manually with Azure CLI:

```powershell
az login
az webapp deploy `
  --resource-group <resource-group> `
  --name <app-name> `
  --src-path .\conference-rooms.zip `
  --type zip
```

Verify:

```text
https://<app-name>.azurewebsites.net/swagger
```

In Azure, the application stores SQLite and data-protection files under `%HOME%\data`, outside `site\wwwroot`. They survive normal restarts and ZIP deployments. Deleting the App Service or its files, storage failure, or SQLite locking problems can still cause data loss.

SQLite on App Service persistent storage is an accepted single-instance demo compromise, not the recommended production architecture. A real production deployment should switch the data provider to Azure SQL or another client/server database.
