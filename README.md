# OrderFlow

Production-oriented order management system: **.NET 8 API** (Clean Architecture, DDD, CQRS/MediatR) plus a **React + TypeScript** operations UI.

The domain enforces a strict order lifecycle:

`Draft → Confirmed → Paid → Shipped → Completed`  
Cancellable from `Draft`, `Confirmed`, or `Paid`. Stock is reserved on confirm and released on cancel.

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
| Orders | CRUD draft + `confirm` / `pay` / `ship` / `complete` / `cancel` |
| Catalog | Customers and products (Admin/Manager write) |
| Dashboard | `GET /api/dashboard` |

JWT access tokens expire in 15 minutes. Refresh tokens are stored as SHA-256 hashes and rotated.

### Seed users

| Email | Password | Role |
| --- | --- | --- |
| admin@orderflow.dev | Admin@123 | Admin |
| manager@orderflow.dev | Manager@123 | Manager |
| operator@orderflow.dev | Operator@123 | Operator |

## Local run

SQL Server must be reachable at `localhost,1433` (Docker Compose provides this).

```bash
docker compose up sqlserver -d
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
dotnet run --project src/OrderFlow.Api
```

Swagger: http://localhost:5088/swagger

Frontend:

```bash
cd frontend
npm install
npm run dev
```

UI: http://localhost:5173

Or the full stack:

```bash
docker compose up --build
```

- API: http://localhost:5088/swagger
- Web: http://localhost:5173

## Tests

```bash
dotnet test
```

## Azure / CI

GitHub Actions builds and tests on every push to `main`. Deployment to Azure App Service runs when these repository secrets exist:

- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

Configure the App Service connection string `DefaultConnection` and `Jwt__Secret` in Azure Configuration.

## Security notes

Change `Jwt:Secret` and the SQL password before any non-local deployment. Seed passwords are for development only.
