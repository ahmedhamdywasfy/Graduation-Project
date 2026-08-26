# Person 2 — Sprint 1: Database Foundation & Horse Core
## Implementation Report

## Architecture Summary

This sprint adds the "Horse Core" slice on top of Person 1's existing Clean
Architecture (Domain → Application → Infrastructure → API), reusing every
cross-cutting concern already in place: Repository Pattern, CQRS/MediatR,
FluentValidation, AutoMapper, the `ApiResponse<T>` wrapper, the global exception
handling middleware, Serilog, and the existing JWT/role-based authorization.
No existing module was rewritten; every new file is additive, and the eight
modified files each received the smallest possible change to wire the new
module in (new DbSets, new DI registrations, a new authorization policy, new
exception-to-status-code mappings, one new seeding call).

New Horse Core follows the exact same layering Person 1 established:
- **Domain**: `Horse` (aggregate root), `Breed`/`Color`/`Gender`/`HorseStatus`
  (lookup entities, mirroring `Role`), `OwnershipHistory`, `HorseImage`, plus a
  new `SoftDeletableAuditableEntity` base class (Horse-specific audit/soft-delete
  fields — Person 1's identity entities don't need these, so nothing about
  `User`/`RefreshToken`/etc. changed).
- **Application**: Commands (Create/Update/Delete/Restore) and Queries
  (GetById/GetAll/Search) under `Horses/`, each with a MediatR handler and a
  FluentValidation validator for format/range rules. Duplicate Microchip/
  Registration Number checks and FK-existence checks are done in the handlers
  via repository calls and thrown as specific domain exceptions — matching the
  exact pattern Person 1 used for `EmailAlreadyRegisteredException`, not
  FluentValidation `MustAsync`.
- **Infrastructure**: EF Core configurations (including a global soft-delete
  query filter on `Horse` and two filtered unique indexes), repositories, and
  additions to `DbSeeder` for the four lookup tables plus new
  `horses.view`/`horses.manage` permission rows.
- **API**: `HorsesController`, and a new `CanManageHorses` authorization policy
  added alongside the existing `RequireAdministrator` policy.

## Modified Components

| File | Change |
|---|---|
| `IApplicationDbContext.cs` | Added 7 new `DbSet<T>` properties for the Horse Core tables |
| `ApplicationDbContext.cs` | Implemented the same 7 `DbSet<T>` properties; updated class doc comment |
| `DependencyInjection.cs` (Infrastructure) | Registered 5 new repository interfaces/implementations |
| `DbSeeder.cs` | Added `SeedHorseLookupDataAsync()` call + method; extended the permission seed list and grant logic |
| `AuthenticationExtensions.cs` | Added the `CanManageHorses` policy (Administrator, Owner, Veterinarian) |
| `ExceptionHandlingMiddleware.cs` | Added 2 new exception → HTTP 400 mappings for Horse validation exceptions |
| `User.cs`, `UserRole.cs` | See "Notable Fix" below — unrelated to Horse Core, but touches Person 1 code |

### Notable fix (not Horse-specific)

While writing unit tests for the new module, I found that `UserRole.Role` is
only ever populated by EF Core's own query materialization — any entity graph
built purely in memory (e.g., in a unit test that never touches a real
`DbContext`) leaves it `null`, meaning code paths like
`user.UserRoles.Select(ur => ur.Role.Name)` (used in `LoginCommandHandler`,
`RefreshTokenCommandHandler`) would NullReferenceException if exercised outside
a real database. **This is not a production bug** — EF Core's own query engine
sets private-setter navigation properties directly during materialization,
bypassing constructors entirely — but it meant Sprint 2's own delivered unit
tests (`LoginCommandHandlerTests`, etc.) were never actually runnable, since
they were never verified against a real SDK. Fixed with a small, backward-
compatible addition: a `UserRole(Guid userId, Role role)` constructor overload
that also wires the navigation property, used by `User.AssignRole`/
`ReplaceRoles`. No existing method signature changed.

## Database Changes

New tables: `Breeds`, `Colors`, `Genders`, `HorseStatuses`, `Horses`,
`HorseImages`, `OwnershipHistories`. No existing table was altered or renamed.

- `Horses.MicrochipNumber` / `Horses.RegistrationNumber`: nullable, with a
  **filtered unique index** (`WHERE ... IS NOT NULL`) so multiple horses can
  each have no microchip/registration without a uniqueness violation.
- `Horses` has a **global EF Core query filter** (`WHERE IsDeleted = 0`) —
  every normal query automatically excludes soft-deleted rows; Restore bypasses
  it explicitly via `IgnoreQueryFilters()`.
- FK delete behavior: `Restrict` for Breed/Color/Gender/Status/CurrentOwner
  (protects reference data and user rows); `Cascade` for HorseImages and
  OwnershipHistories (dependent children of Horse) — matching the pattern
  already documented in the approved v0.1 §13 schema notes.
- `OwnershipHistories` has two FKs to `Users` (PreviousOwner, NewOwner), both
  set to `Restrict` — SQL Server disallows multiple cascade paths to the same
  table, so this avoids a migration-time error.

## Migration Details

**No migration file is included in this package.** This sandbox has no .NET SDK
or NuGet access, so I could not run `dotnet ef migrations add` myself — the
same limitation flagged in Sprint 1/2. Generate it as the first step after
applying this patch:

```bash
dotnet ef migrations add Person2Sprint1_HorseCore \
  --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API
dotnet ef database update \
  --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API
```

## New APIs

| Endpoint | Method | Access |
|---|---|---|
| `/api/v1/horses` | POST | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{id}` | PUT | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{id}` | DELETE (soft) | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{id}/restore` | POST | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{id}` | GET | Any authenticated user |
| `/api/v1/horses` | GET (paged) | Any authenticated user |
| `/api/v1/horses/search` | GET (paged, filtered) | Any authenticated user |

## Testing Summary

- **Unit tests** (`tests/SmartHorse.Application.Tests/Horses/`): Create (5
  cases: success, unknown breed, duplicate microchip, duplicate registration,
  unknown owner, default-status resolution), Update (3 cases), Delete (3
  cases, including double-delete guard), Restore (2 cases), Search (2 cases,
  including full-criteria pass-through verification).
- **Integration tests** (`tests/SmartHorse.API.IntegrationTests/HorsesControllerTests.cs`):
  Create as Owner (201), Create as Buyer (403), Create without token (401),
  Create with future birth date (400), read access for a read-only role (200),
  delete→get(404)→restore→get(200) round trip, search with a breed filter.
- Both test projects reuse Sprint 2's existing `CustomWebApplicationFactory`
  (EF Core InMemory) and `Moq`/`FluentAssertions` conventions unchanged.

**I could not actually execute `dotnet test` in this environment** — see
BUILD_REPORT.md for what was and wasn't verified.

## Integration Notes

- Horse Core's write-access policy (`CanManageHorses`) is role-based, using the
  exact same `RequireRole` mechanism as Person 1's `RequireAdministrator`
  policy — not the fine-grained `Permissions`/`RolePermissions` tables, even
  though this sprint also seeds `horses.view`/`horses.manage` permission rows.
  Those rows are seeded now so a future fine-grained authorization handler
  (v0.2 §2.2) has data to work with, but nothing evaluates them yet — the same
  is true of every other seeded permission in this codebase today.
- `CreateHorseCommandHandler` validates the given `OwnerId` via the existing
  `IUserRepository` — no new coupling was introduced beyond an interface
  Person 1 already exposed.

## Known Limitations

- **Ownership transfer** is out of scope for this sprint. `Horse.CurrentOwnerId`
  is set once at creation (with a corresponding `OwnershipHistory` row); there
  is no "transfer ownership" command/endpoint yet. A future sprint should add
  one, reusing `Horse.RecordOwnership`, which already supports it.
- **Horse image upload** has no endpoint yet — the `HorseImages` table,
  entity, and `Horse.AddImage` domain method exist, but nothing calls them from
  the API. A future sprint should reuse the existing `IFileStorageService`
  abstraction from Person 1 Sprint 2 (the same one avatar upload uses).
- **No `/api/v1/breeds` (or colors/genders/statuses) listing endpoints** were
  added — the repositories exist (with `GetAllAsync`) but there's no
  controller exposing them for building dropdowns client-side. Low-risk,
  low-effort addition for whichever sprint needs it first.
- I could not run `dotnet build`/`dotnet test`/`dotnet ef migrations add`
  myself in this environment (no .NET SDK, no NuGet access) — see
  BUILD_REPORT.md.
- **Unrelated, found during this sprint**: the repository currently has live
  diagnostic/debugging code (`ChangeTrackerDiagnosticsInterceptor`,
  `SqlDiagnosticsInterceptor`, verbose SQL logging) investigating an unresolved
  `DbUpdateConcurrencyException` on Login, explicitly marked `TEMPORARY` in
  `appsettings.json` and `Program.cs`. This is untouched by this sprint (out of
  scope), but it logs sensitive request data (emails, token hashes, IPs) and
  should be resolved and removed before any further release.

## Future Recommendations

1. Add a dedicated `TransferOwnershipCommand` (Person 2 Sprint 2?), reusing
   `Horse.RecordOwnership`.
2. Add horse image upload (`POST /api/v1/horses/{id}/images`), reusing
   `IFileStorageService` and `Horse.AddImage`.
3. Add lookup-listing endpoints (`GET /api/v1/breeds`, `/colors`, `/genders`,
   `/horse-statuses`) for client-side dropdowns.
4. Consider whether `CanManageHorses` should eventually be evaluated via the
   `Permissions`/`RolePermissions` tables instead of a hardcoded role list,
   consistent with v0.2 §2's fine-grained-permission design goal.
5. Resolve and remove the unrelated diagnostic interceptors described above.
