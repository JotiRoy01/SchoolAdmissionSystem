# School Admission System

A Dockerized school admission system built for the Onnorokom Projukti
take-home assignment.

## Technology

- Backend: C# / ASP.NET Core (.NET 10)
- Database: PostgreSQL 17 with Entity Framework Core and Npgsql
- Frontend: React 19, TypeScript, and Vite
- Production frontend server: Nginx
- Container orchestration: Docker Compose

## Run the complete application with Docker

Docker Desktop is the only required dependency. From the repository root,
run:

```powershell
docker compose up --build
```

Docker Compose starts:

| Service | URL / port | Purpose |
|---|---|---|
| Frontend | <http://localhost:3000> | React application |
| Backend API | <http://localhost:8080> | ASP.NET Core API |
| PostgreSQL | `localhost:5432` | Persistent application database |

The backend exposes `GET /health` for container readiness checks. PostgreSQL
data is stored in the `postgres_data` named volume, so stopping and restarting
the containers does not remove classes or applications.

Stop the containers without deleting data:

```powershell
docker compose down
```

To intentionally remove the database volume as well:

```powershell
docker compose down --volumes
```

## Configuration

Compose reads optional values from a root `.env` file. Start from
[.env.example](./.env.example):

```text
POSTGRES_DB=school_admission
POSTGRES_USER=admission_user
POSTGRES_PASSWORD=admission_dev_password
POSTGRES_PORT=5432
HOLD_DURATION_SECONDS=600
VITE_API_BASE_URL=http://localhost:8080
```

Configuration values:

- `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD` configure PostgreSQL
  and the backend connection string.
- `POSTGRES_PORT` changes the host port exposed for PostgreSQL.
- `HOLD_DURATION_SECONDS` controls how long a seat remains held. The default
  is 600 seconds.
- `VITE_API_BASE_URL` is the frontend build-time API base URL. Rebuild the
  frontend after changing it:

```powershell
docker compose build frontend
docker compose up
```

The backend always listens on `http://localhost:8080` when run through
Compose, as required by the assignment.

## Using the application

No login is required. Enter the student's 17-digit birth registration number
in the student identity field.

### Parent/student flow

1. Open the frontend at <http://localhost:3000>.
2. Enter a valid 17-digit student ID.
3. View each class/shift and its total, held, confirmed, and available seats.
4. Select **Apply** to hold an available seat.
5. View the hold status and expiry countdown.
6. Select **Pay admission fee**. This simulates a successful payment; no real
   payment gateway is used.
7. Confirm the admission.
8. Use **My Applications** to view the student's applications.
9. Cancel a held application when needed.

The class seat counts refresh automatically. Hold status and expiry are
refreshed without requiring a full page reload. Student identity is persisted
in the browser so the student's active applications remain visible after a
refresh or browser restart.

### Admin flow

Open the admin screen from the application and provide a class/shift name and
seat count. Each shift is represented as a separate class record, for example:

- `Class 6 Morning Shift`
- `Class 6 Day Shift`

The assignment does not require authentication, so the admin screen is
intentionally unauthenticated.

## API contract

Base URL: `http://localhost:8080`

All request and response bodies are JSON except successful cancellation,
which returns `204 No Content`. Requests that operate on a student application
send the student's 17-digit birth registration number in the
`X-Student-Id` header.

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/classes` | Create a class/shift |
| `GET` | `/classes` | List classes with seat counts |
| `GET` | `/classes/{classId}` | Get one class and its seat counts |
| `POST` | `/classes/{classId}/holds` | Hold an available seat |
| `GET` | `/holds/{holdId}` | Get the current application status |
| `POST` | `/holds/{holdId}/confirm` | Confirm a paid application |
| `DELETE` | `/holds/{holdId}` | Cancel an application |
| `GET` | `/me/holds` | List applications for the student |

The implementation also exposes the payment simulation endpoint used by the
UI:

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/holds/{holdId}/payment` | Mark the admission fee as paid |

Hold status values are `HELD`, `CONFIRMED`, `EXPIRED`, and `CANCELLED`.
Creating a hold when no seat is available returns `409 Conflict`.

Example:

```powershell
$headers = @{ "X-Student-Id" = "20152691234567890" }

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8080/classes `
  -ContentType "application/json" `
  -Body '{"name":"Class 6 Morning Shift","totalSeats":120}'

Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:8080/me/holds `
  -Headers $headers
```

## Business rules

- Held seats plus confirmed seats never exceed the class capacity.
- A hold expires after `HOLD_DURATION_SECONDS` and returns its seat.
- Expiration is processed automatically by the background worker and enforced
  during API operations.
- A student cannot have more than one active held or confirmed application
  for the same class.
- Only the student who owns a non-expired `HELD` application can confirm it.
- Confirmation is idempotent; repeating it does not increment confirmed seats
  more than once.
- PostgreSQL row locking serializes seat allocation for concurrent requests,
  including multiple backend processes sharing the same database.

See [ASSUMPTIONS.md](./ASSUMPTIONS.md) for decisions where the assignment
brief was not explicit.

## Tests and verification

Run the backend tests:

```powershell
dotnet test
```

The backend test suite includes:

- Class creation
- Invalid student ID validation
- Concurrent applications proving that capacity is not exceeded

Run frontend checks:

```powershell
Set-Location frontend
npm ci
npm run build
npm run lint
```

Run all Docker services before running the backend API tests, because the
tests call the API at `http://localhost:8080`:

```powershell
docker compose up --build -d
dotnet test
```

## Repository structure

```text
backend/
  SchoolAdmission.Api/       ASP.NET Core API, EF Core, migrations
  SchoolAdmission.Api.Tests/ API and concurrency tests
frontend/                    React/Vite application
docker-compose.yml           PostgreSQL, backend, and frontend services
ASSUMPTIONS.md               Assignment assumptions and design decisions
AI_USAGE.md                  AI-assisted development disclosure
```

## AI usage

AI-assisted development details, including validation of generated
suggestions and identified corrections, are documented in
[AI_USAGE.md](./AI_USAGE.md).
