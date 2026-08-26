# Person 2 — Sprint 2: Build Report

## ⚠️ Environment Limitation (read first, same as every prior sprint)

This package was authored without .NET SDK or NuGet registry access. I could
not run `dotnet build`, `dotnet test`, `dotnet ef migrations add`, or launch
the API. Everything below is a careful manual/static review, not an executed
build. **Please run the commands in "Required Verification" and report back
any errors** — I will fix them immediately, the same loop that resolved the
AutoMapper/EF Core reference issue in Sprint 1 and the `PropertyAccessMode`
namespace issue afterward.

## What Was Actually Verified

- **The exact bug class that broke this repo's build twice before** (a bare
  `Domain.X`/`Namespace.X` reference missing its full path, which only fails
  at actual compile time) — grepped the entire new/modified surface area for
  this sprint specifically. None found. Where I had genuine doubt about
  nested-namespace resolution rules (`Images.CloudinarySettings` referenced
  from within `SmartHorse.Infrastructure`), I didn't rely on reasoning about
  it — I added an explicit `using` directive and switched to unqualified
  names, removing the ambiguity entirely rather than trusting my own analysis
  a third time.
- **Signature consistency**: cross-checked `IHorseRepository`'s 12 methods
  against `HorseRepository`'s implementation one-by-one (all present, matching
  signatures); checked every `Horse`/`OwnershipHistory`/`HorseImage`
  constructor and mutator call site against its declaration.
- **Brace balance** on the four most heavily-edited files (`Horse.cs`,
  `DependencyInjection.cs`, `HorseImagesController.cs`,
  `ExceptionHandlingMiddleware.cs`) — all balanced.
- **Backward compatibility with Sprint 1**: grepped for every call site of
  `Horse.AddImage` (signature changed from 2 args to 9) and confirmed no
  Sprint 1 file — including Sprint 1's own unit tests — calls the old
  signature. `Horse.RecordOwnership`'s new "close out the previous stint"
  logic is skipped entirely when `previousOwnerId` is null, so Sprint 1's
  `CreateHorseCommandHandlerTests` (which calls it with `previousOwnerId: null`)
  is unaffected.
- **Cascade-path safety**: both new self-referencing Horse FKs (Father,
  Mother) are `Restrict`, avoiding the same "multiple cascade paths" class of
  SQL Server migration error already avoided for `OwnershipHistory`'s two
  User FKs in Sprint 1.
- **Exception correctness**: caught and fixed my own mistake before finishing
  — `InvalidImageDimensionsException` originally had one constructor reused
  for both "too small" and "too large" cases, which would have produced a
  wrong error message on the too-large path. Replaced with
  `TooSmall(...)`/`TooLarge(...)` factory methods.

## Build Status

**NOT BUILT.**

## Warnings / Errors

None known — review-based assessment only.

## Migration Verification

**NOT GENERATED.** See IMPLEMENTATION_REPORT.md's "Migration Details" — note
that this will be the *first* migration for Horse Core + Ownership + Lineage +
Images combined, since Sprint 1's migration was never generated either.

## Swagger Verification

**NOT VERIFIED live.** All three new controllers follow the exact
`[ApiController]`/XML-doc/`[ProducesResponseType]` pattern already confirmed
working for `HorsesController`.

## Unit Test Results

**NOT RUN.** 12 new test cases across `Ownership/`, `Lineage/`, `HorseImages/`
in `SmartHorse.Application.Tests`.

## Integration Test Results

**NOT RUN.** 13 new test cases across `OwnershipControllerTests.cs`,
`LineageControllerTests.cs`, `HorseImagesControllerTests.cs`. These rely on a
new `FakeImageStorageService` registered in `CustomWebApplicationFactory` in
place of the real Cloudinary implementation — no test hits the real Cloudinary
API, so no real credentials are needed to run them.

## Required Verification (please run and report results)

```bash
# 1. Apply this package
tar -xzf Person2_Sprint2.tar.gz
chmod +x apply_person2_sprint2.sh
./apply_person2_sprint2.sh

# 2. Generate and review the migration
dotnet ef migrations add Person2_HorseCoreAndOwnershipLineageImages \
  --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API

# 3. Build
dotnet build

# 4. Apply the migration
dotnet ef database update \
  --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API

# 5. Run tests
dotnet test

# 6. Run the API and check /swagger manually
dotnet run --project src/SmartHorse.API
```

If you don't have real Cloudinary credentials yet, leave `Cloudinary:*` empty
in configuration for now — every endpoint except actual image upload will work
fine, and the integration tests don't need real credentials at all (they use
the fake). Only a live `POST /api/v1/horses/{id}/images` call against the
running API (not the test suite) needs real Cloudinary credentials configured.

If step 3 or step 5 fails, paste the exact output back to me and I will fix it
in the next iteration.
