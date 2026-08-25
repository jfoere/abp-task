# Conference Rooms API — Implementation Plan

## 1. Goal and Scope

Build a small ASP.NET Core Web API for managing conference rooms, finding availability, creating bookings, calculating prices, and viewing basic business reports.

The implementation will stay close to the assignment. Booking cancellation, customer accounts, a UI, exports, and other unrequested features are out of scope and can be documented as future improvements.

## 2. Agreed Technical Choices

- ASP.NET Core Web API targeting .NET 8.
- Controllers rather than Minimal APIs.
- EF Core for persistence.
- SQLite locally and in the deployed demo.
- Windows Azure App Service, deployed manually without Docker.
- One App Service instance with scaling disabled.
- Public Swagger UI at `/swagger`.
- Four configured API-key identities: two administrators and two customers.
- Automatic EF Core migrations and idempotent reference-data seeding at startup.
- Automated tests will be added after the main implementation is complete.

## 3. Solution Structure

```text
ConferenceRooms.sln
src/
  ConferenceRooms.Api/             Controllers, authentication, Swagger, HTTP concerns
  ConferenceRooms.Application/     Use cases, pricing, availability, reports, contracts
  ConferenceRooms.Infrastructure/  EF Core, SQLite, migrations, repositories, seed data
tests/
  ConferenceRooms.Tests/           Unit and integration tests added after implementation
```

Dependencies point inward: `Api` and `Infrastructure` depend on `Application`; `Application` does not depend on ASP.NET Core or EF Core.

## 4. Core Data Model

### Room

- ID
- Name
- Capacity
- Base hourly rate in UAH
- Soft-deletion state and timestamps
- Supported services

### Service

- ID
- Name
- Fixed price in UAH

The global service catalog is seeded with Projector, Wi-Fi, and Sound. Rooms select the services they support. Separate service-management endpoints are out of scope.

### Booking

- ID
- Room ID
- Start and end timestamps stored in UTC
- API-key identity that created the booking
- Room-rate snapshot
- Room charge, service charge, and total charge
- Creation timestamp
- Selected-service price snapshots

Price snapshots ensure later room or service price changes do not alter historical bookings or reports.

## 5. Authentication and Authorization

Clients send an API key in the `X-API-Key` header. Each configured key maps to a principal name and one role.

- `Admin`: room CRUD, reports, availability search, and booking.
- `Customer`: availability search and booking.
- Swagger UI is public and provides an **Authorize** button.
- Secret values are stored in .NET user secrets locally and Azure App Settings in the deployed app.
- No secret key values are committed to Git.

The initial configuration will contain identities named `admin-1`, `admin-2`, `customer-1`, and `customer-2`.

## 6. Booking and Pricing Rules

- The business timezone is `Europe/Kyiv`.
- Request timestamps must use ISO 8601 and include an offset.
- Timestamps are converted to UTC for storage and overlap comparisons.
- Pricing is calculated after converting timestamps to the business timezone.
- Bookings must start and end on full local hours.
- Minimum duration is one hour; duration is a whole number of hours.
- Bookings must remain within `06:00–23:00` on one local calendar day.
- Back-to-back bookings are allowed; intervals use `[start, end)` semantics.
- Optional services are charged once per booking.
- A room can only be booked with services that it supports.

Hourly room pricing:

- `06:00–09:00`: 10% morning discount.
- `09:00–12:00`: standard rate.
- `12:00–14:00`: 15% peak surcharge; this overrides the standard rate.
- `14:00–18:00`: standard rate.
- `18:00–23:00`: 20% evening discount.

Bookings spanning multiple periods are split into hourly segments and each segment uses its applicable multiplier.

## 7. Room Deletion

Deleting a room performs a soft delete.

- The first successful delete returns `204 No Content`.
- Deleted rooms are excluded from reads, searches, updates, and new bookings.
- Later operations treat a deleted room as missing and return `404 Not Found`.
- Existing booking records remain available for history and reports.
- Future bookings do not block room deletion.

## 8. API Surface

### Rooms

- `POST /api/rooms` — Admin
- `GET /api/rooms` — public room catalog
- `GET /api/rooms/{id}` — public room details
- `PUT /api/rooms/{id}` — Admin
- `DELETE /api/rooms/{id}` — Admin
- `GET /api/rooms/available?startTime=...&durationHours=...&capacity=...` — public availability search

### Bookings

- `POST /api/bookings` — Customer or Admin

The response contains the booking ID, time range, hourly price breakdown, selected-service prices, and total.

### Reports

All report endpoints require the Admin role and accept `from` and `to` parameters.

- `GET /api/reports/revenue`
- `GET /api/reports/utilization`
- `GET /api/reports/services`

Reports return concise JSON only; charts and exports are out of scope.

## 9. Reliability and Error Handling

- Validate request models and business rules before persistence.
- Return RFC 7807 Problem Details for errors.
- Recheck availability inside a database transaction before inserting a booking.
- Reject overlapping bookings with `409 Conflict`.
- Return `400 Bad Request` for invalid time ranges, unsupported services, and other validation failures.
- Log unexpected failures without exposing stack traces or secrets.
- Add basic ASP.NET Core rate limiting to public endpoints.

## 10. Database Initialization and Storage

- Apply pending migrations with `Database.MigrateAsync()` during startup.
- Seed initial rooms and services only when missing.
- Store the local database under an ignored application-data directory.
- Store the Azure database at `%HOME%\data\conference-rooms.db`, outside the deployed `site\wwwroot` content.
- Do not include a database file in deployment artifacts.

The deployed SQLite setup is explicitly a single-instance demo compromise. The README will document its network-backed storage and concurrency limitations and recommend a client/server database for real production use.

## 11. Build Order

1. Pin the .NET 8 SDK and scaffold the solution and project references.
2. Add domain models, application contracts, shared errors, and request/response DTOs.
3. Add EF Core SQLite persistence, mappings, migration, and idempotent seed data.
4. Add API-key authentication, role policies, Problem Details, rate limiting, and Swagger security configuration.
5. Implement room CRUD and soft deletion.
6. Implement availability search and overlap rules.
7. Implement segmented pricing, price snapshots, and transactional booking creation.
8. Implement the three report endpoints.
9. Complete Swagger descriptions, configuration examples, README, and manual Azure deployment instructions.
10. Add the focused automated test suite and run the final verification pass.

The solution should compile after every implementation step. Swagger smoke testing will be used during development; the automated suite is deliberately scheduled after the main implementation.

## 12. Final Verification

- Build succeeds from a clean checkout.
- Migrations create a new database and do not overwrite existing data.
- Seed data is not duplicated after restart.
- Admin and Customer keys enforce the expected permissions.
- Availability and double-booking behavior are correct.
- Pricing is correct across every rate boundary.
- Soft-deleted rooms behave as missing while historical bookings remain.
- Reports match seeded test bookings.
- Swagger is publicly reachable after manual Azure deployment.
- Data survives a normal Azure App Service restart.
