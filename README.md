# OrderFlow

Production-oriented order management: **.NET 8 API** (Clean Architecture, DDD, CQRS/MediatR) plus a **React + TypeScript** operations UI.

The domain enforces a strict order lifecycle:

`Draft → Confirmed → Paid → Shipped → Completed`  
Cancellable from `Draft`, `Confirmed`, or `Paid`. Stock is reserved on confirm and released on cancel. Domain events are dispatched in-process after each successful commit.

## Architecture

```
src/OrderFlow.Domain          Entities, value objects, domain events, invariants
src/OrderFlow.Application     CQRS (MediatR), FluentValidation pipeline, DTOs
src/OrderFlow.Infrastructure  EF Core, SQL Server, Unit of Work, BCrypt, JWT
src/OrderFlow.Api             Controllers, Serilog, Swagger, exception middleware
frontend                      React + Vite operations console
tests                         xUnit + Moq + FluentAssertions + WebApplicationFactory
```

## API

| Area | Endpoints |
| --- | --- |
| Auth | `POST /api/auth/login`, `register`, `refresh`, `revoke`, `GET /api/auth/me` |
| Orders | Draft CRUD + `confirm` / `pay` / `ship` / `complete` / `cancel` |
| Catalog | Customers and products (Admin/Manager write) |
| Dashboard | `GET /api/dashboard` |

JWT access tokens expire in 15 minutes. Refresh tokens are stored as SHA-256 hashes and rotated. The UI refreshes the session automatically.

### Seed users

| Email | Password | Role |
| --- | --- | --- |
| admin@orderflow.dev | Admin@123 | Admin |
| manager@orderflow.dev | Manager@123 | Manager |
| operator@orderflow.dev | Operator@123 | Operator |

## Local run

```bash
docker compose up --build
```

- API / Swagger: http://localhost:5088/swagger
- Web: http://localhost:5173

Without Docker for the UI (API still needs SQL Server on `localhost,1433`):

```bash
docker compose up sqlserver -d
dotnet run --project src/OrderFlow.Api
cd frontend && npm install && npm run dev
```

Migrations run automatically on API startup.

## Tests

```bash
dotnet test
```

## CI / Azure

GitHub Actions builds and tests the API and frontend on every push to `main`.

Azure App Service deploy is **manual**: run the `CI` workflow with `workflow_dispatch` after configuring:

- GitHub secrets: `AZURE_WEBAPP_NAME`, `AZURE_WEBAPP_PUBLISH_PROFILE`
- App Service settings: `ConnectionStrings__DefaultConnection`, `Jwt__Secret`

## Security notes

Change `Jwt:Secret` and the SQL password before any non-local deployment. Seed passwords are for development only.
