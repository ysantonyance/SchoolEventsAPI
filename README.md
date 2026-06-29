# SchoolEventsAPI

A school event management system where organizers create and publish events, students register, and a waitlist system automatically promotes students when spots open up. Async notifications are handled by a separate worker process.

---

## Tech Stack

- **Backend:** C# / ASP.NET Core (.NET 10)
- **Database:** SQL Server (Entity Framework Core)
- **Auth:** JWT Bearer tokens + BCrypt password hashing
- **Frontend:** React + Vite + Axios
- **Worker:** ASP.NET Core BackgroundService (separate process)

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (local or remote)
- Node.js 18+

### 1. Clone the repository

```bash
git clone https://github.com/ysantonyance/SchoolEventsAPI.git
cd SchoolEventsAPI
```

### 2. Configure the database

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SchoolEventsDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "school-events-super-secret-key-2026-make-it-long",
    "Issuer": "SchoolEventsAPI",
    "Audience": "SchoolEventsAPI"
  }
}
```

### 3. Run migrations

```bash
dotnet ef database update
```

### 4. Start the API

```bash
dotnet run --project SchoolEventsAPI
# API runs on http://localhost:5057
```

### 5. Start the Worker

```bash
dotnet run -- --worker
# Worker polls NotificationJobs every 5 seconds
```

### 6. Start the Frontend

```bash
cd school-events-frontend
npm install
npm run dev
# Frontend runs on http://localhost:5173 or :5174
```

---

## User Roles

| Role | How to create | Permissions |
|------|--------------|-------------|
| `student` | Register via `/auth/register` or UI | Browse events, register, cancel, view own registrations |
| `organizer` | Register then manually update role in DB | Create/edit/publish/cancel events, view registrations and waitlist |

### Creating an organizer account

1. Register normally via the UI or API
2. Run this SQL query:
```sql
UPDATE Users SET Role = 'organizer' WHERE Email = 'your@email.com';
```
3. Log in again to get a new token with the organizer role

---

## REST API

### Authentication

| Method | Path | Description |
|--------|------|-------------|
| POST | `/auth/register` | Register as student |
| POST | `/auth/login` | Login → returns JWT token |

### Events

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/events` | Organizer | Create DRAFT event |
| GET | `/events` | Any | Students: published only. Organizer: own events |
| GET | `/events/{id}` | Any | Event details with confirmed/waitlist counts |
| PUT | `/events/{id}` | Organizer | Edit DRAFT event (owner only) |
| POST | `/events/{id}/publish` | Organizer | DRAFT → PUBLISHED |
| POST | `/events/{id}/cancel` | Organizer | Cancel event |
| GET | `/events/{id}/registrations` | Organizer | Confirmed registrations (owner only) |
| GET | `/events/{id}/waitlist` | Organizer | Ordered waitlist (owner only) |

### Registrations

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/events/{id}/registrations` | Student | Register → CONFIRMED or WAITLISTED |
| DELETE | `/registrations/{id}` | Student | Cancel own registration + auto-promote waitlist |
| GET | `/registrations/me` | Student | Own registrations + waitlist position |

---

## Key Design Decisions

### No overbooking under concurrency
Registration uses `IsolationLevel.Serializable` transactions to prevent two simultaneous requests from both getting the last available seat.

### Waitlist (FIFO)
Waitlist ordering is determined by `RegisteredAt` timestamp. When a confirmed seat is freed, the student with the earliest `RegisteredAt` is automatically promoted.

### Async notifications (Transactional Outbox)
After every registration-related change, a `NotificationJob` row is inserted in the **same transaction** as the registration. This guarantees the job is never lost even if the worker crashes.

### Idempotency
Each `NotificationJob` has a unique `IdempotencyKey` (`registrationId:jobType`). The database enforces uniqueness, preventing duplicate notifications on retry.

### Password security
Passwords are hashed using BCrypt (slow hash, not reversible encryption).

### Input validation
All DTOs use Data Annotations and `IValidatableObject` for cross-field validation (e.g. EndTime must be after StartTime).

---

## Notification Job Types

| Type | Trigger |
|------|---------|
| `RegistrationConfirmed` | Student gets a confirmed seat |
| `RegistrationWaitlisted` | Student is placed on waitlist |
| `WaitlistPromoted` | Student promoted from waitlist to confirmed |
| `RegistrationCancelled` | Student cancels their registration |

---

## Security

- Passwords: BCrypt slow hashing
- Auth: JWT Bearer tokens (1 hour expiry)
- SQL injection: prevented via EF Core parameterized queries
- XSS: React auto-escapes all rendered content
- Authorization: every sensitive endpoint checks role + ownership
- Students cannot see other students' registrations
