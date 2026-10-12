# 🎓 College Event Registration and Management System

A modern, full-stack, enterprise-grade web application built with **ASP.NET Core (.NET 10)**, designed for colleges and universities to publish events, manage registrations, automate attendance tracking, and provide students with a seamless event discovery and ticketing experience.

Designed with a sleek **Purple & White** SaaS aesthetic, clean architecture, and role-based access control.

---

## 🌟 Key Features

### 👨‍🎓 Student Portal
- **Browse & Search:** Explore upcoming campus events with dynamic search by keyword and category filters (Technical, Cultural, Sports, Workshop, Seminar).
- **Seat Availability & Deadlines:** Real-time visibility into available seats, capacity meters, and countdowns before registration deadlines.
- **One-Click Registration:** Safe registration with duplicate-prevention and seat exhaustion validation.
- **My Tickets & QR-Style View:** View active registrations, check status (`Approved`, `Pending`, `Cancelled`), and cancel tickets prior to the event date.
- **Registration History:** Review past attended events and attendance records.
- **Student Profile:** View and update personal information (Name, Student ID, Department, Year of Study, Contact Phone).

### 🛡️ Admin Portal
- **KPI Metrics Dashboard:** Real-time summary cards displaying Total Events, Active Registrations, Registered Students, and Total Attendance.
- **Visual Analytics:** Interactive **Chart.js** charts visualizing Registrations by Category and Monthly Participation Trends.
- **Event Lifecycle Management:** Full CRUD (Create, Read, Update, Delete) for college events, including date/time, venue, capacity limits, registration cutoffs, and custom poster image uploads.
- **Registration Management:** Review participant lists with ability to approve, reject, or cancel registrations.
- **Attendance Tracking:** Mark participants as `Present` or `Absent` on event day.
- **CSV Reports & Export:** One-click data export of all or filtered registrations to CSV for administrative records and offline analysis.
- **Student Directory:** Searchable roster of registered student accounts.

---

## 🎨 Design & Theme Palette

The application uses a custom-crafted UI styled with **Bootstrap 5** and custom CSS variables:

| Element | Hex Color | Role |
| :--- | :--- | :--- |
| **Primary Purple** | `#6C3CE9` | Primary action buttons, brand icons, active state indicators |
| **Dark Purple** | `#4527A0` | Hero headers, gradient accents, admin sidebar brand |
| **Light Lavender** | `#F3EEFF` | Subtle card badges, active menu item highlights, hover fills |
| **Pure White** | `#FFFFFF` | Card surfaces, modals, elevated surfaces |
| **Soft Background**| `#F8F7FC` | Page backdrop for comfortable visual contrast |
| **Primary Text** | `#242038` | Deep readable typography |
| **Success / Error**| `#22C55E` / `#EF4444` | Toast alerts, attendance status pills |

---

## 🏗️ Architecture & Project Structure

The solution follows a clean 4-tier separation of concerns, complemented by an automated test suite:

```
CollegeEventManagement/
├── CollegeEventManagement.Core/           # Domain Layer
│   ├── Constants/                         # AppRoles, EventCategories, RegistrationStatus
│   ├── DTOs/                              # Data Transfer Objects (Auth, Events, Registrations, Dashboard)
│   ├── Entities/                          # ApplicationUser, Event, EventRegistration
│   └── Interfaces/                        # IEventRepository, IRegistrationRepository, IDashboardRepository
│
├── CollegeEventManagement.Infrastructure/ # Data & Persistence Layer
│   ├── Data/                              # ApplicationDbContext (EF Core)
│   ├── Migrations/                        # Entity Framework Core Code-First Migrations
│   ├── Repositories/                      # EventRepository, RegistrationRepository, DashboardRepository
│   └── Seed/                              # DbInitializer (automatic admin, students, and 10 sample events)
│
├── CollegeEventManagement.API/            # ASP.NET Core Web API (Backend)
│   ├── Controllers/                       # AuthController, EventsController, RegistrationsController, AdminController
│   ├── Middleware/                        # GlobalExceptionMiddleware
│   ├── Services/                          # TokenService (JWT Bearer Token Generation)
│   └── Program.cs                         # API Pipeline, Swagger/OpenAPI, EF Core SQL Server registration
│
├── CollegeEventManagement.Web/            # ASP.NET Core MVC (Frontend)
│   ├── Controllers/                       # HomeController, EventsController, AccountController, StudentController, AdminController
│   ├── Services/                          # ApiService (IHttpClientFactory client communicating with Web API)
│   ├── Views/                             # Razor Views with responsive layouts and purple theme
│   ├── wwwroot/                           # CSS, JavaScript, Bootstrap 5, Bootstrap Icons, Event Posters
│   └── Program.cs                         # Cookie Authentication, HTTP Client configuration
│
└── CollegeEventManagement.Tests/          # Unit & Integration Tests (xUnit)
    ├── EventBusinessRulesTests.cs         # Event domain rules (IsDeadlinePassed, IsFull, etc.)
    ├── EventRepositoryTests.cs            # In-memory EF Core repository CRUD tests
    └── RegistrationAndReportTests.cs      # Concurrency, duplicate registrations, and CSV export tests
```

---

## 💻 Tech Stack

- **Framework:** .NET 10.0 (C# 13)
- **Frontend:** ASP.NET Core MVC with Razor Views, Bootstrap 5, Bootstrap Icons, Chart.js
- **Backend API:** ASP.NET Core Web API with RESTful endpoints
- **Database:** Microsoft SQL Server (LocalDB or SQL Server Express)
- **ORM:** Entity Framework Core 10 (Code-First with Migrations)
- **Authentication:** ASP.NET Core Identity + JWT Bearer Tokens (Web API) & Cookie Authentication (MVC Frontend)
- **API Documentation:** Swagger / OpenAPI UI
- **Testing:** xUnit, Moq, EF Core InMemory Database Provider

---

## 🔑 Default Seeded Accounts

The database comes pre-populated with ready-to-use accounts for immediate testing:

| Role | Email | Password | Details |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@collegeevents.edu` | `Admin@Secure123!` | Full access to Admin Portal, event CRUD, attendance, and CSV export. |
| **Student** | `student@collegeevents.edu` | `Student@Secure123!` | Pre-registered student (Atharv Sharma, CS2024001) with 3 sample registrations. |
| **Sample Student 2** | `priya.patel@collegeevents.edu` | `Student@Secure123!` | Pre-registered student (Priya Patel, IT2024045). |

> *Tip: You can also register a brand-new student account directly on the web registration page!*

---

## 🚀 Getting Started (Step-by-Step)

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or Visual Studio 2022 v17.12+)
- SQL Server LocalDB (installed by default with Visual Studio Data Storage workload) or SQL Server Express / Developer edition.

### 2. Verify Database Connection
The default connection string in both `CollegeEventManagement.API/appsettings.json` and `CollegeEventManagement.Web/appsettings.json` points to SQL Server LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CollegeEventDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

If using a named SQL Server instance (e.g. `localhost\SQLEXPRESS`), simply update `Server=localhost\\SQLEXPRESS` in `CollegeEventManagement.API/appsettings.json`.

### 3. Apply Database Migrations (Automatic & Manual)
The API includes an automated migration and seeder on application startup. To manually run migrations via CLI:

```powershell
dotnet ef database update --project CollegeEventManagement.Infrastructure --startup-project CollegeEventManagement.API
```

### 4. Run the Backend Web API
Open a terminal in the solution directory and run:

```powershell
dotnet run --project CollegeEventManagement.API\CollegeEventManagement.API.csproj --urls "http://localhost:5172"
```

The Web API will start on **`http://localhost:5172`**.
- **Swagger Documentation:** Visit [http://localhost:5172/swagger](http://localhost:5172/swagger) to explore and interact with all API endpoints.

### 5. Run the Frontend MVC Application
Open a second terminal in the solution directory and run:

```powershell
dotnet run --project CollegeEventManagement.Web\CollegeEventManagement.Web.csproj --urls "http://localhost:5041"
```

The MVC Web Application will launch on **`http://localhost:5041`**.
- Open your browser and navigate to: **[http://localhost:5041](http://localhost:5041)**

---

## 🧪 Running Automated Tests

A comprehensive suite of 12 unit and integration tests verifies business logic, duplicate prevention, and repository operations:

```powershell
dotnet test CollegeEventManagement.sln
```

Expected output:
```
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```

---

## 📡 API Endpoints Overview

| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Register a new student account | None |
| `POST` | `/api/auth/login` | Authenticate and obtain JWT token | None |
| `GET` | `/api/auth/profile` | Retrieve authenticated user profile | Bearer |
| `PUT` | `/api/auth/profile` | Update user profile details | Bearer |
| `GET` | `/api/events` | List published events (with filter, search, paging) | None |
| `GET` | `/api/events/{id}` | Get event details by ID | Optional Bearer |
| `POST` | `/api/events` | Create new event (Admin only) | Admin |
| `PUT` | `/api/events/{id}` | Update existing event | Admin |
| `DELETE`| `/api/events/{id}` | Delete event | Admin |
| `POST` | `/api/registrations` | Register authenticated student for an event | Student |
| `GET` | `/api/registrations/my`| Retrieve student's registered events | Student |
| `DELETE`| `/api/registrations/{id}`| Cancel student's registration | Student |
| `GET` | `/api/admin/dashboard`| Fetch KPIs, category counts, monthly trends | Admin |
| `GET` | `/api/admin/registrations`| Get all event registrations with filters | Admin |
| `PUT` | `/api/admin/registrations/{id}/status` | Update registration status (Approved/Rejected) | Admin |
| `PUT` | `/api/admin/registrations/{id}/attendance` | Mark attendance (Present/Absent) | Admin |
| `GET` | `/api/admin/registrations/export` | Download CSV registration export | Admin |
| `GET` | `/api/admin/students` | Get list of registered students | Admin |

---

## 🛠️ Troubleshooting

- **LocalDB not started:** Run `sqllocaldb start MSSQLLocalDB` or `sqllocaldb info MSSQLLocalDB` in PowerShell.
- **Port already in use:** If port `5172` or `5041` is occupied, change the `--urls` parameter when running `dotnet run` and update `ApiBaseUrl` in `CollegeEventManagement.Web/appsettings.json`.
- **Database Reset (If ever needed):**
  ```powershell
  dotnet ef database drop --project CollegeEventManagement.Infrastructure --startup-project CollegeEventManagement.API
  dotnet ef database update --project CollegeEventManagement.Infrastructure --startup-project CollegeEventManagement.API
  ```

---

## 📄 License
This project is open-source and created for educational purposes. Feel free to use and extend it!
