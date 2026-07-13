# Smart Horse Management System — Backend
## Sprint 1: Backend Foundation & Authentication (Person 1)

This solution implements **only** Authentication and User Management, per the
Sprint 1 scope, on top of the approved v0.1/v0.2 architecture. See the bottom of
this file for what's deliberately not implemented yet.

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is fine for development) or a connection string to any SQL Server instance
- An RSA key pair for JWT signing (see below)

## 1. Generate an RSA key pair for JWT (RS256)

```bash
openssl genrsa -out jwt-private.pem 2048
openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem
```

Do **not** commit these files. Load them into configuration via .NET user secrets
(development) or environment variables / Key Vault (production):

```bash
cd src/SmartHorse.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:PrivateKeyPem" "$(cat ../../jwt-private.pem)"
dotnet user-secrets set "Jwt:PublicKeyPem" "$(cat ../../jwt-public.pem)"
dotnet user-secrets set "Seed:AdminEmail" "admin@smarthorse.local"
dotnet user-secrets set "Seed:AdminPassword" "ChangeMe123!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=SmartHorseDb;Trusted_Connection=True;TrustServerCertificate=True"
```

## 2. Create the initial migration and database

```bash
cd src/SmartHorse.API
dotnet tool install --global dotnet-ef   # if not already installed
dotnet ef migrations add InitialIdentitySchema --project ../SmartHorse.Infrastructure --startup-project .
dotnet ef database update --project ../SmartHorse.Infrastructure --startup-project .
```

## 3. Run

```bash
dotnet run --project src/SmartHorse.API
```

On first run, `DbSeeder` will:
- Apply pending migrations automatically (`Database.MigrateAsync()`),
- Seed the six roles (Owner, Veterinarian, Trainer, Worker, Buyer, Administrator),
- Seed a baseline Permissions set + grant them all to Administrator,
- Seed one Administrator account from `Seed:AdminEmail` / `Seed:AdminPassword`.

Swagger UI opens automatically at `/swagger` in Development.

## Solution Structure

See the "Folder Structure" section of the Sprint 1 delivery summary for the full
annotated tree.

## Not Implemented in This Sprint

Per the Sprint 1 scope, the following are **not** included and are reserved for
later sprints: Horse Management, Medical Records, Marketplace, Training, AI
Services, Flutter mobile app, and the full multi-channel Notifications system
(v0.2 Section 4). A stub `IEmailService` (logs instead of sending) exists only to
support the Forgot Password flow end-to-end.
