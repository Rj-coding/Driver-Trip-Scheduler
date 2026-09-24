# 📚 Driver Trip Scheduler — Repository Knowledge Base

> A complete, code-verified reference for the Driver Trip Scheduler system.
> Backend: ASP.NET Core 8 Web API · Frontend: React 19 + Vite SPA · DB: SQL Server (EF Core 9).
> Auth: JWT Bearer with roles `Manager` and `Driver`.

---

## 1. Backend Architecture

Strict 3-tier layering with dependency inversion at each seam:

```
Controllers  →  Services (business rules)  →  Repositories (EF Core)  →  AppDbContext  →  SQL Server
     │                                                                        ▲
   DTOs  ◄──────────── AutoMapper (Trip) / manual mapping (Driver/Vehicle/User)
```

- **Controllers** — HTTP boundary, routing, `[Authorize]` role gates, status-code mapping.
- **Services** — business rules & validation; return **result tuples** `(bool isSuccess, string errorMessage, T entity)` instead of throwing.
- **Repositories** — encapsulate all EF Core access behind interfaces.
- **AppDbContext** — entity configuration and relationships.
- **JwtHelper** (Singleton) — token generation.
- **Composition root**: `Program.cs` wires DI, JWT auth, CORS, Swagger, and the middleware pipeline.

**Middleware pipeline** (`Program.cs`):
```
UseCors("AllowFrontend") → [dev] UseSwagger/UI → UseAuthentication → UseAuthorization → UseHttpsRedirection → MapControllers
```

---

## 2. Frontend Architecture

Single-page React app; state via hooks; Bootstrap UI; toasts via `react-toastify`.

```
main.jsx (BrowserRouter)
  └─ App.jsx (Routes + ToastContainer)
       ├─ / .............. LandingPage      (public)
       ├─ /register ...... Register         (public)  → api.post('/Auth/register')
       ├─ /login ......... Login            (public)  → api.post('/Auth/login'), stores token+role
       ├─ /dashboard ..... Dashboard        (role-gated cards)
       ├─ /drivers ....... ManageDrivers    (Manager) → /api/Driver CRUD
       ├─ /vehicles ...... ManageVehicles   (Manager) → /api/Vehicle CRUD
       ├─ /trips ......... AssignTrip       (Manager) → POST /api/Trip
       ├─ /viewtrips ..... ViewTrips        (Manager) → GET/PUT/DELETE + filter
       └─ /mytrips ....... MyTrips          (Driver)  → GET /api/Trip/filter?driverName=<token username>
```

- **Auth storage**: JWT + role in `localStorage`. Only `Login.jsx`/`Register.jsx` use the shared axios instance ([api.js](Frontend/driver-trip-scheduler-frontend/src/api.js)); other pages hardcode `http://localhost:5038/api` with raw `axios`/`fetch` and manually attach `Authorization: Bearer <token>`.
- **No axios interceptors. No route guards** — each protected page checks `localStorage` itself (`Dashboard` redirects to `/login` if missing).
- **Test hook**: `?test=true` sets `localStorage.testEnv`; `AssignTrip` swaps `datetime-local` inputs to `text` for testability.

---

## 3. Database Architecture

- **Provider**: SQL Server via EF Core 9 (`AppDbContext`).
- **Connection**: `appsettings.json` → `ConnectionStrings:DriverTripDB` (`Server=APH000257\SQLEXPRESS;Database=DriverTripDB;Trusted_Connection=True;TrustServerCertificate=True`).
- **DbSets**: `Users, Drivers, Vehicles, Cities, Areas, Trips`.
- ⚠️ **No `Migrations/` folder is committed** in the workspace, though the README references `dotnet ef database update`. City/Area seed data is not in the repo.

---

## 4. Folder Structure

```
DriverTripSchedulerMain/
├── Backend/
│   ├── DriverTripBackendProject/          # ASP.NET Core 8 Web API (the app)
│   │   ├── Controllers/  Auth, Driver, Vehicle, Trip, City
│   │   ├── Service/      {Driver,Trip,User,Vehicle}Services (I* + impl)
│   │   ├── Repository/   {Driver,Trip,User,Vehicle}Repo (I* + impl)
│   │   ├── Models/       User, Driver, Vehicle, City, Area, Trip
│   │   ├── DTO/          Users, Drivers, Vehicles, Trips
│   │   ├── Data/         AppDbContext, AppDbContextFactory  (no Migrations/)
│   │   ├── Helpers/      JwtHelper
│   │   ├── MappingProfiles/ MappingProfile (Trip mappings)
│   │   ├── Program.cs · appsettings.json · Properties/launchSettings.json
│   ├── DriverTripScheduler.Tests/         # MSTest + Moq (see §15 — external ref)
│   └── CoverageReport/                    # generated HTML (artifact)
├── Frontend/
│   └── driver-trip-scheduler-frontend/    # React 19 + Vite SPA
│       └── src/ main.jsx · App.jsx · api.js · pages/ · __mocks__/
└── README.md · PROJECT_KNOWLEDGE_BASE.md
```

---

## 5. Entity Relationships

```
User  (standalone auth entity: UserId, Username, PasswordHash, Role)

City (1) ───< Area (∞)                         [FK Area.CityId]
Driver (1) ──── (1) Vehicle                    [FK Vehicle.DriverId, DeleteBehavior.Restrict]
Driver (1) ───< Trip (∞)                       [FK Trip.DriverId]
Vehicle (1) ──< Trip (∞)                       [FK Trip.VehicleId]
Trip (∞) >──── (1) OriginCity / OriginArea / DestinationCity / DestinationArea   [all Restrict]
```

- `[JsonIgnore]` on `Driver.Trips` and `City.Areas` breaks serialization cycles.
- All four Trip location FKs use `Restrict` to avoid multiple cascade paths.
- Entity note: `Driver.Phone` (entity) ↔ `PhoneNumber` (DTO).

---

## 6. Request Lifecycle

```
Browser (page) ──HTTP + Bearer──▶ Kestrel
  → CORS (only http://localhost:5173)
  → AuthN (JWT: issuer/audience/lifetime/signature)
  → AuthZ ([Authorize(Roles=...)])
  → Controller action (model binding → DTO)
      → Service (validation, mapping)
          → Repository → AppDbContext → SQL
      ← entity / result tuple
  → ActionResult: Ok / Created / Conflict(409) / NotFound(404) / BadRequest(400) / Unauthorized(401)
◀ JSON DTO → React state → Bootstrap render + toast
```

---

## 7. Authentication Flow

```
Login.jsx ──POST /api/Auth/login──▶ AuthController.Login
  → UserService.LoginAsync
      → UserRepository.GetByUsernameAsync
      → SHA-256(password) == PasswordHash ?
          yes → JwtHelper.GenerateToken
                 claims: ClaimTypes.Name (username), ClaimTypes.Role
                 HmacSha256, expires 2h, issuer == audience == "DriverTripSystem"
          → UserResponseDTO { Username, Role, Token }  → 200 OK
          no  → null → 401 Unauthorized
Login.jsx: localStorage token+role; api default Authorization header; navigate('/dashboard')
```

- **Password hashing**: unsalted SHA-256 → Base64 ([UserService.cs](Backend/DriverTripBackendProject/Service/UserServices/UserService.cs)).
- No refresh tokens; logout = `localStorage.clear()`.

### Authorization
Enforced **server-side** by the JWT `Role` claim and `[Authorize(Roles="Manager")]` / `"Manager,Driver"`. Frontend role logic is cosmetic (which cards/pages to show).

---

## 8. Trip Creation Flow

```
AssignTrip.jsx (Manager)
  ├─ GET /City, /Driver, /Vehicle (Promise.all)
  ├─ cascading GET /City/{id}/areas on city change (origin + destination)
  ├─ client: startTime < endTime; token present
  └─ POST /api/Trip {ids, times} + Bearer
        ▼ TripController.CreateTrip [Authorize Manager]
        ▼ TripService.AddTripAsync — sequential guards:
            1. start/end not in past
            2. end > start
            3. IsAreaInCity(originArea, originCity)
            4. IsAreaInCity(destArea, destCity)
            5. IsDriverAvailable(driverId)          # no trip with TripEndTime > now
            6. HasOverlappingTripForDriver(...)
            7. HasOverlappingTripForVehicle(...)
          fail → return (false, message) → 409 Conflict → toast
          pass → map TripDTO→Trip → AddTripAsync (CreatedAt=Now)
                 → re-fetch GetTripWithDetailsByIdAsync (6 Includes)
                 → 201 Created (TripResponseDTO) → navigate('/viewtrips')
```

---

## 9. Trip Update Flow

```
ViewTrips.jsx (Manager) → Edit modal → PUT /api/Trip/{id} {TripUpdateDTO} + Bearer
  ▼ TripController.UpdateTrip  (guard: id == dto.TripId else 400)
  ▼ TripService.UpdateTripAsync:
      • GetTripByIdAsync(dto.TripId)   (existingTrip fetched first)
      • past-time check   ← runs BEFORE the null check (order quirk)
      • end > start check
      • null check → "Trip not found."
      • IsDriverAvailable(driverId, excludeTripId = TripId)
      • HasOverlappingTripForDriver(..., excludeTripId = TripId)
      • HasOverlappingTripForVehicle(..., excludeTripId = TripId)
      • ⚠ does NOT re-run IsAreaInCity (unlike create)
      • map dto → existingTrip → UpdateTripAsync → re-fetch with details
  → 200 OK (full trip) / 409 Conflict (message)
```

**Key differences from create**: uses `excludeTripId` so a trip never conflicts with itself; **skips area-in-city revalidation**.

---

## 10. Driver Assignment (Driver Lifecycle)

```
Create (Manager, POST /Driver: {name, phoneNumber})
  → optional 1:1 Vehicle link via PUT /Driver/{id} (DriverUpdateDTO carries VehicleId)
  → assignable to trips (blocked by IsDriverAvailable while ANY future trip exists)
  → Delete (Manager, DELETE /Driver/{id})
```
- `DriverService.UpdateAsync(id, dto)` builds a `Driver{Id,Name,Phone,VehicleId}` → `DriverRepository.UpdateAsync` loads existing, copies fields, returns `false` if not found → controller returns 404.
- Driver↔Vehicle FK is `Restrict`, so a linked driver/vehicle cannot be deleted while the relationship holds.

---

## 11. Vehicle Assignment (Vehicle Lifecycle)

```
Create (Manager, POST /Vehicle: VehicleCreateDTO{VehicleNumber,Type})
  → VehicleService.AddAsync forces DriverId = null
  → CreatedAtAction(id = VehicleNumber)   # note: uses number, not numeric id
Update (Manager, PUT /Vehicle/{id}: VehicleDTO, guard id==VehicleId)
  → loads existing, sets VehicleNumber/Type/DriverId (CAN assign a driver here)
Delete (Manager, DELETE /Vehicle/{id})
```

---

## 12. Validation Rules

| Layer | Rule | Location |
|---|---|---|
| Frontend | `required` fields; `startTime < endTime`; token present | AssignTrip / forms |
| Controller | `id == dto.TripId` (Trip PUT); `id == VehicleId` (Vehicle PUT); ≥1 filter param; `ModelState` (Vehicle POST) | Controllers |
| Service (Trip) | not past · end>start · area-in-city (create) · driver available · driver overlap · vehicle overlap | TripService |
| Service (User) | duplicate username rejected | UserService |

**Overlap predicate** (same driver/vehicle, optional `excludeTripId`), any of:
`(start ≥ t.Start && start < t.End)` OR `(end > t.Start && end ≤ t.End)` OR `(start ≤ t.Start && end ≥ t.End)`.

---

## 13. Business Rules

- A driver may hold **only one active/upcoming trip** — `IsDriverAvailable` blocks assignment if any trip has `TripEndTime > now` (stricter than pure overlap).
- No driver or vehicle may have time-overlapping trips.
- Trips cannot be scheduled in the past; end must be after start.
- Areas must belong to their selected city (enforced on create; **not re-checked on update**).
- Trip status (**Not Started / Running / Completed**) is **derived on the frontend** from times — not stored.
- Roles: **Manager** = full control; **Driver** = read own trips only (matched by JWT username against `driverName`).

---

## 14. Dependency Graph & DI Registrations

`Program.cs`:
| Service | Lifetime |
|---|---|
| `AppDbContext` (SqlServer, `DriverTripDB`) | Scoped |
| `IUserRepository → UserRepository` | Scoped |
| `ITripRepository → TripRepository` | Scoped |
| `IDriverRepository → DriverRepository` | Scoped |
| `IVehicleRepository → VehicleRepository` | Scoped |
| `IUserService → UserService` | Scoped |
| `ITripService → TripService` | Scoped |
| `IDriverService → DriverService` | Scoped |
| `IVehicleService → VehicleService` | Scoped |
| `JwtHelper` | Singleton |
| AutoMapper(`MappingProfile`) · JwtBearer · Authorization · SwaggerGen(+Bearer) · CORS `AllowFrontend` | — |

```
AuthController   → IUserService   → UserService   → IUserRepository   → AppDbContext
                                    └→ JwtHelper
DriverController → IDriverService → DriverService → IDriverRepository → AppDbContext
VehicleController→ IVehicleService→ VehicleService→ IVehicleRepository→ AppDbContext
TripController   → ITripService   → TripService   → ITripRepository   → AppDbContext
                                    └→ IMapper (MappingProfile)
CityController   → AppDbContext   (bypasses service/repository — only layering exception)
```

---

## 15. API Inventory

Base URL: `http://localhost:5038/api`

| # | Method | Route | Auth | Request DTO | Response |
|---|---|---|---|---|---|
| 1 | POST | `/Auth/register` | Public | `UserRegisterDTO{Username,Password,Role}` | 200 text / 400 |
| 2 | POST | `/Auth/login` | Public | `UserLoginDTO{Username,Password}` | `UserResponseDTO{Username,Role,Token}` / 401 |
| 3 | GET | `/Driver` | Public | — | `DriverDTO[]` |
| 4 | GET | `/Driver/{id}` | Public | — | `DriverDTO` / 404 |
| 5 | POST | `/Driver` | Manager | `DriverCreateDTO{Name,PhoneNumber}` | 200 msg |
| 6 | PUT | `/Driver/{id}` | Any authed | `DriverUpdateDTO{Name,PhoneNumber,VehicleId?}` | 200 / 404 |
| 7 | DELETE | `/Driver/{id}` | Manager | — | 200 msg |
| 8 | GET | `/Vehicle` | Public | — | `VehicleDTO[]` |
| 9 | GET | `/Vehicle/{id}` | Public | — | `VehicleDTO` / 404 |
| 10 | POST | `/Vehicle` | Manager | `VehicleCreateDTO{VehicleNumber,Type}` | 201 |
| 11 | PUT | `/Vehicle/{id}` | Manager | `VehicleDTO` (id==VehicleId) | 200 / 400 |
| 12 | DELETE | `/Vehicle/{id}` | Manager | — | 200 msg |
| 13 | GET | `/Trip` | Public | — | `TripResponseDTO[]` |
| 14 | POST | `/Trip` | Manager | `TripDTO` | 201 / 409 |
| 15 | PUT | `/Trip/{id}` | Manager | `TripUpdateDTO` (id==TripId) | 200 / 400 / 409 |
| 16 | DELETE | `/Trip/{id}` | Manager | — | 200 / 404 |
| 17 | GET | `/Trip/filter?driverName=&vehicleNumber=` | Manager, Driver | query (≥1) | `TripResponseDTO[]` / 400 |
| 18 | GET | `/City` | Public | — | `City[]` |
| 19 | GET | `/City/{id}/areas` | Public | — | `Area[]` |

**DTO field reference**
- `TripDTO`: OriginCityId, OriginAreaId, DestinationCityId, DestinationAreaId, DriverId, VehicleId, TripStartTime, TripEndTime.
- `TripUpdateDTO`: `TripDTO` + `TripId`.
- `TripResponseDTO`: ids + `*Name` (Origin/Destination City & Area, DriverName, VehicleNumber) + times + `CreatedAt`.

---

## 16. Test Coverage Summary

### Backend — MSTest + Moq ([DriverTripScheduler.Tests](Backend/DriverTripScheduler.Tests))
> ⚠️ **Reference mismatch:** the test `.csproj` references
> `..\..\..\DriverTripSchedulerBackend\DriverTripSchedulerBackend\DriverTripSchedulerBackend.csproj`
> and uses `DriverTripSchedulerBackend.*` namespaces — an **external project outside this workspace**, not `DriverTripBackendProject`. The workspace backend appears to be a rename/copy (coverage HTML class names are also `DriverTripSchedulerBackend_*`). As-is, these tests will not compile against `DriverTripBackendProject` without fixing the reference/namespaces.

| Suite | Cases |
|---|---|
| `TripServiceTests` (create) | past-time, end<start, invalid origin area, invalid dest area, driver unavailable, driver overlap, vehicle overlap, success, GetAll maps DTOs |
| `UpdateTripTest` | not found, past-time, end<start, driver unavailable, driver overlap, vehicle overlap, success, GetTripsByFilter |
| `DriverServiceTests` | Add maps entity, GetAll, GetById, Update maps fields, Delete delegates |
| `VehicleServiceTests` | GetAll, GetById exists/null, Add forces DriverId=null, Update exists/not-found, Delete |
| `UserServiceTests` | Register new/duplicate, Login correct/wrong password (JWT via in-memory config) |
| `AuthTests` (functional) | register+login returns token, duplicate username fails, invalid credentials → 401 (real `HttpClient` → localhost:5038) |
| `TripFunctionalTests` (functional) | end-to-end trip endpoints |

### Frontend — Vitest + React Testing Library (`src/pages/__tests__`)
`AssignTrip`, `Dashboard`, `Login`, `Register`, `ViewTrips` — axios/`api` mocked; localStorage token spied; `?test=true` mode.

---

## 17. Configuration & Environment

- **Backend**: `appsettings.json` holds the SQL connection and **hardcoded** `Jwt:Key`/`Jwt:Issuer`. `ASPNETCORE_ENVIRONMENT=Development` gates Swagger. Ports from `launchSettings.json`: http `5038`, https `7168`.
- **Frontend**: base URL hardcoded in `api.js`. `.env` only defines a JS `isTest` flag (not a Vite build var).

---

## 18. Known Gaps / Observations (facts, not recommendations)

1. **No `Migrations/` folder** committed despite README instructions.
2. **No background jobs/schedulers** (`IHostedService`, Quartz, cron) — all logic is synchronous in the request path.
3. **Test project references an external backend** (`DriverTripSchedulerBackend`) with mismatched namespaces.
4. `UpdateTripAsync` **skips `IsAreaInCity`** revalidation.
5. `UpdateTripAsync` checks past-time **before** the null/existence check.
6. **No global exception handler / ProblemDetails**; unhandled errors surface as raw 500s.
7. **No structured logging**; default `ILogger` config only.
8. **Security**: unsalted SHA-256 passwords; hardcoded JWT key + connection string; several anonymous GET endpoints.
9. **CityController** bypasses the service/repository layers.
10. **Frontend**: no axios interceptors, no route guards; inconsistent client usage (shared `api` vs raw `axios`/`fetch`).
11. `ViewTrips.jsx` has a leftover `toast.error("Test toast on load")` on mount.

---

*Generated from a full read of backend controllers, services, repositories, models, DTOs, interfaces, `AppDbContext`, `Program.cs`, config, mapping profile, all frontend pages, and the test suites.*
