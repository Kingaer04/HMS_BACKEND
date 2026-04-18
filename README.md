# HMS Backend — Auth Module

## Project Structure

```
HMS/
├── HMS.sln
├── HMS.API/                         ← Entry point ONLY (no controllers here)
│   ├── Program.cs                   ← DI, middleware, AddApplicationPart()
│   ├── appsettings.json
│   ├── HMS.API.csproj
│   └── Middleware/
│       └── GlobalExceptionMiddleware.cs
│
├── HMS.Presentation/                ← ALL controllers live here
│   ├── Controllers/
│   │   └── AuthController.cs
│   └── HMS.Presentation.csproj      ← Class library (not web)
│
├── HMS.Service.Contracts/           ← Interfaces only
│   ├── IServiceContracts.cs         ← IAuthService, ITokenService, INhisVerificationService, ILoggerService
│   └── HMS.Service.Contracts.csproj
│
├── HMS.Service/                     ← Business logic implementations
│   ├── AuthService.cs
│   ├── TokenService.cs
│   ├── NhisVerificationService.cs   ← MOCK — swap for real API later
│   └── HMS.Service.csproj
│
├── HMS.Repository/                  ← EF Core
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   └── HMS.Repository.csproj
│
├── HMS.Entities/                    ← Domain models and enums
│   ├── Models/
│   │   ├── Hospital.cs
│   │   ├── ApplicationUser.cs
│   │   └── NhisVerificationResult.cs
│   ├── Enums/
│   │   └── UserRole.cs
│   └── HMS.Entities.csproj
│
├── HMS.Shared/                      ← DTOs and response wrappers
│   ├── DTOs/Auth/
│   │   ├── HospitalAuthDto.cs
│   │   ├── DoctorAuthDto.cs
│   │   └── ReceptionistAuthDto.cs
│   ├── Responses/
│   │   └── AuthResponseDto.cs
│   └── HMS.Shared.csproj
│
└── HMS.LoggerService/               ← Logging
    ├── LoggerService.cs
    └── HMS.LoggerService.csproj
```

## Key Design Decision — Separate Presentation Layer

Controllers live in `HMS.Presentation` (a plain class library), not in `HMS.API`.

`HMS.API/Program.cs` discovers them using:
```csharp
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(HMS.Presentation.Controllers.AuthController).Assembly);
```

This keeps the API entry point clean — it only handles configuration, DI registration, and middleware.

## Getting Started

### 1. Update connection string
In `HMS.API/appsettings.json`:
```json
"DefaultConnection": "Server=YOUR_SERVER;Database=HospitalManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 2. Run migrations
In Visual Studio — Package Manager Console (default project: HMS.Repository):
```
Add-Migration InitialCreate
Update-Database
```

Or via CLI from the solution root:
```bash
dotnet ef migrations add InitialCreate --project HMS.Repository --startup-project HMS.API
dotnet ef database update --project HMS.Repository --startup-project HMS.API
```

### 3. Run the API
Set `HMS.API` as startup project and run. Swagger available at `/swagger`.

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/verify-uid` | None | Verify NHIS UID before registration |
| POST | `/api/auth/register/hospital` | None | Register hospital + admin account |
| POST | `/api/auth/login/hospital` | None | Admin login — UID + Password |
| POST | `/api/auth/register/doctor` | HospitalAdmin | Register a doctor |
| POST | `/api/auth/login/doctor` | None | Doctor login — Email + Password |
| POST | `/api/auth/register/receptionist` | HospitalAdmin | Register a receptionist |
| POST | `/api/auth/login/receptionist` | None | Receptionist login — Email + Password |
| POST | `/api/auth/refresh-token` | None | Get new access token |
| POST | `/api/auth/logout` | Any | Revoke refresh token |

## Mock NHIS UIDs for Testing

| UID | Hospital |
|-----|----------|
| NHIS-0001-LG | Lagos General Hospital |
| NHIS-0002-AB | Abuja National Hospital |
| NHIS-0003-KN | Kano State Hospital |
| NHIS-0004-PH | Port Harcourt Teaching Hospital |
| NHIS-0005-IB | University College Hospital Ibadan |

## Swapping NHIS Mock for Real API
Only touch `HMS.Service/NhisVerificationService.cs` — replace the dictionary lookup
with your real HTTP call. Nothing else needs to change.
