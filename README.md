# 🚚 Driver Trip Scheduler

A full-stack web application for scheduling and managing driver trips. Managers can manage drivers, vehicles and assign trips (with automatic conflict detection), while drivers can view their own assigned trips. Built with an **ASP.NET Core 8 Web API** backend and a **React 19 + Vite** frontend, secured with **JWT-based role authentication**.

---

## 📑 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Project Structure](#-project-structure)
- [Data Model](#-data-model)
- [API Reference](#-api-reference)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
- [Authentication & Roles](#-authentication--roles)
- [Testing](#-testing)
- [Security Notes](#-security-notes)
- [License](#-license)

---

## ✨ Features

- 🔐 **JWT authentication** with role-based access (`Manager` and `Driver`).
- 🧑‍✈️ **Driver management** – create, view, update and delete drivers.
- 🚗 **Vehicle management** – manage the fleet with one-to-one driver assignment.
- 🗓️ **Trip assignment** with cascading City → Area selection for origin and destination.
- 🛡️ **Smart conflict validation** on trips:
  - Trip cannot be scheduled in the past.
  - End time must be after start time.
  - Selected area must belong to the selected city.
  - Driver must be available (no active/upcoming trips).
  - No overlapping trips for the same driver or vehicle.
- 🔎 **Filtering** of trips by driver name and/or vehicle number.
- 👤 **Driver self-service view** – drivers see only the trips assigned to them.
- 🔔 Toast notifications and responsive Bootstrap UI.

---

## 🛠 Tech Stack

### Backend
| Technology | Version |
|---|---|
| .NET / ASP.NET Core | 8.0 |
| Entity Framework Core | 9.0.7 |
| SQL Server | Express / LocalDB |
| AutoMapper | 12.0.1 |
| JWT Bearer Authentication | 8.0.5 |
| Swashbuckle (Swagger) | 6.6.2 |

### Frontend
| Technology | Version |
|---|---|
| React | 19.1.0 |
| Vite | 7.0.4 |
| React Router | 7.7.1 |
| Axios | 1.11.0 |
| React Bootstrap / Bootstrap | 2.10.10 / 5.3.7 |
| jwt-decode | 4.0.0 |
| date-fns | 4.1.0 |
| react-toastify | 11.0.5 |
| Vitest + React Testing Library | 3.2.4 / 16.3.0 |

---

## 🏗 Architecture

The backend follows a clean **3-tier layered architecture** with clear separation of concerns:

```
Controllers  →  Services (business logic)  →  Repositories (data access)  →  EF Core / SQL Server
```

- **Controllers** handle HTTP requests, routing and authorization.
- **Services** contain business rules and validation (e.g. trip conflict checks).
- **Repositories** encapsulate all Entity Framework Core data access.
- **AutoMapper** maps between entities and DTOs.
- **DTOs** keep the API contract decoupled from the database entities.

The frontend is a **single-page application** that communicates with the API over REST using Axios, storing the JWT and role in `localStorage`.

---

## 📂 Project Structure

```
DriverTripSchedulerMain/
├── Backend/
│   └── DriverTripBackendProject/        # ASP.NET Core 8 Web API
│       ├── Controllers/                 # Auth, Driver, Vehicle, Trip, City
│       ├── Service/                     # Business logic (interfaces + impl)
│       ├── Repository/                  # EF Core data access (interfaces + impl)
│       ├── Models/                      # Entities: User, Driver, Vehicle, Trip, City, Area
│       ├── DTO/                         # Request/response DTOs
│       ├── Data/                        # AppDbContext + migrations
│       ├── Helpers/                     # JwtHelper (token generation)
│       ├── MappingProfiles/             # AutoMapper profiles
│       ├── Program.cs                   # DI, middleware, auth, CORS, Swagger
│       └── appsettings.json             # Connection string + JWT config
│
├── Frontend/
│   └── driver-trip-scheduler-frontend/  # React 19 + Vite SPA
│       └── src/
│           ├── pages/                   # Login, Register, Dashboard, ManageDrivers,
│           │                            # ManageVehicles, AssignTrip, ViewTrips,
│           │                            # MyTrips, LandingPage
│           ├── api.js                   # Axios instance (baseURL)
│           ├── App.jsx                  # Routes + ToastContainer
│           └── main.jsx                 # App entry (BrowserRouter)
│
└── README.md
```

---

## 🗃 Data Model

| Entity | Key Fields | Relationships |
|---|---|---|
| **User** | UserId, Username, PasswordHash, Role | Authentication entity |
| **Driver** | DriverId, Name, PhoneNumber, VehicleId | 1:1 Vehicle, 1:many Trip |
| **Vehicle** | VehicleId, VehicleNumber, Type, DriverId | 1:1 Driver, 1:many Trip |
| **City** | CityId, Name | 1:many Area |
| **Area** | AreaId, Name, CityId | many:1 City |
| **Trip** | TripId, Origin/Destination City & Area, DriverId, VehicleId, TripStartTime, TripEndTime, CreatedAt | many:1 Driver, Vehicle, City (×2), Area (×2) |

**Relationship rules:**
- `City → Areas`: one-to-many (cascade delete).
- `Driver ⇄ Vehicle`: one-to-one (restrict delete).
- `Trip` references origin/destination cities and areas with restrict delete behavior.

---

## 🔌 API Reference

Base URL: `http://localhost:5038/api`

### Auth — `/api/auth`
| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/register` | Public | Register a new user (username, password, role) |
| POST | `/login` | Public | Authenticate and receive a JWT token |

### Drivers — `/api/driver`
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Public | Get all drivers |
| GET | `/{id}` | Public | Get driver by ID |
| POST | `/` | Manager | Create a driver |
| PUT | `/{id}` | Public | Update a driver |
| DELETE | `/{id}` | Manager | Delete a driver |

### Vehicles — `/api/vehicle`
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Public | Get all vehicles |
| GET | `/{id}` | Public | Get vehicle by ID |
| POST | `/` | Manager | Create a vehicle |
| PUT | `/{id}` | Manager | Update a vehicle |
| DELETE | `/{id}` | Manager | Delete a vehicle |

### Trips — `/api/trip`
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Public | Get all trips with full details |
| POST | `/` | Manager | Create a trip (with conflict validation) |
| PUT | `/{id}` | Manager | Update a trip (with conflict validation) |
| DELETE | `/{id}` | Manager | Delete a trip |
| GET | `/filter?driverName=&vehicleNumber=` | Manager / Driver | Filter trips |

### Cities — `/api/city`
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Public | Get all cities |
| GET | `/{id}/areas` | Public | Get areas for a city |

> 📖 Full interactive documentation is available via **Swagger UI** at `http://localhost:5038/swagger` when running in Development.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [SQL Server](https://www.microsoft.com/sql-server) (Express/LocalDB) + optional [SSMS](https://learn.microsoft.com/sql/ssms/)

### Backend Setup

```bash
cd Backend/DriverTripBackendProject
```

1. Update the connection string in `appsettings.json` to point to your SQL Server instance:

   ```json
   "ConnectionStrings": {
     "DriverTripDB": "Server=YOUR_SERVER;Database=DriverTripDB;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

2. Restore packages, apply migrations and run:

   ```bash
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

   The API will be available at **`http://localhost:5038`** and Swagger at **`http://localhost:5038/swagger`**.

> 💡 If the `dotnet ef` command is not found, install the tool with `dotnet tool install --global dotnet-ef`.

### Frontend Setup

```bash
cd Frontend/driver-trip-scheduler-frontend
npm install
npm run dev
```

The app runs at **`http://localhost:5173`** (already whitelisted in the backend CORS policy).

The API base URL is configured in `src/api.js`:

```js
const api = axios.create({ baseURL: "http://localhost:5038/api" });
```

**Available scripts:**

| Command | Description |
|---|---|
| `npm run dev` | Start the Vite dev server |
| `npm run build` | Production build |
| `npm run preview` | Preview the production build |
| `npm run lint` | Run ESLint |
| `npm test` | Run unit tests once (Vitest) |
| `npm run test:ui` | Run Vitest in watch mode |

---

## 🔐 Authentication & Roles

1. Register a user via `/api/auth/register` (or the Register page), choosing a role: **Manager** or **Driver**.
2. Log in via `/api/auth/login` to receive a JWT token (valid for **2 hours**).
3. The token is stored in `localStorage` and sent as `Authorization: Bearer <token>` on API calls.

| Role | Capabilities |
|---|---|
| **Manager** | Full access — manage drivers, vehicles, and create/update/delete/assign trips |
| **Driver** | View own assigned trips (via the filter endpoint) |

---

## 🧪 Testing

### Frontend (Vitest + React Testing Library)

```bash
cd Frontend/driver-trip-scheduler-frontend
npm test
```

Tests live in `src/pages/__tests__/` and cover Login, Register, Dashboard, AssignTrip and ViewTrips. API calls are mocked via `src/__mocks__/api.js`.

### Backend (MSTest)

```bash
cd Backend/DriverTripScheduler.Tests
dotnet test
```

The solution includes unit and functional tests with code coverage reports (Coverlet / ReportGenerator).

---

## 🔒 Security Notes

The following are known areas to harden before a production deployment:

- **Password hashing** currently uses unsalted SHA256 — migrate to a salted algorithm such as **BCrypt** or **PBKDF2**.
- **Secrets** (JWT key and DB connection string) are stored in `appsettings.json` — move them to **environment variables** or **user secrets** and rotate the JWT key.
- **Route protection** on the frontend is client-side only — role enforcement is done by the backend, but consider centralizing token handling in an Axios interceptor.

---

## 📄 License

Copyright © 2026 **Rajnish Kumar Singh**. All rights reserved.

This project is provided for educational/demonstration purposes. Add a license of your choice (e.g. MIT) before public distribution.
