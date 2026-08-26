# Person 2 — Sprint 2: Ownership, Lineage & Image Management
## Implementation Report

## Summary

This sprint adds three modules on top of the existing Horse Core (Person 2
Sprint 1): **Ownership** (transfer, timeline, corrections), **Lineage**
(father/mother, family tree, circular-relationship prevention), and **Horse
Images** (upload/replace/delete/reorder/main-image via Cloudinary, behind a
storage-agnostic abstraction). Every new piece follows the exact patterns
already established in this repository — CQRS/MediatR, Repository Pattern,
FluentValidation for format rules with handler-level checks for anything
needing a DB round trip, the `ApiResponse<T>` wrapper, and the existing
`CanManageHorses` policy (Administrator/Owner/Veterinarian) reused as-is for
every write endpoint in all three new modules, since Sprint 2's access rule is
identical to Sprint 1's.

## Architecture Decisions

- **Ownership** extends the existing `OwnershipHistory` entity (added Sprint 1)
  rather than introducing a parallel table — it already modeled almost
  everything Sprint 2 needs (previous/new owner, notes, a timestamp). Two
  additions: `SaleDate` (closes out a stint) and soft-delete support (the
  entity now derives from `SoftDeletableAuditableEntity` instead of
  `BaseEntity`). `Horse.RecordOwnership` — already the single place that
  writes ownership records since Sprint 1 — now also closes out the
  previously-active record's `SaleDate` in the same call, so both the very
  first registration and every later transfer go through one domain method.
- **Lineage** uses two nullable self-referencing FKs (`FatherId`/`MotherId`)
  on `Horse` rather than a separate join table, since a horse has at most one
  father and one mother. Circular-relationship prevention is a breadth-first
  ancestor walk (`IHorseRepository.GetAncestorIdsAsync`), batched per
  generation rather than one query per node, bounded by
  `Horse.MaxLineageDepth` (20) as a hard safety cap. The family tree endpoint
  builds its recursive response in the Application layer (repeated
  single-level queries) rather than one deep EF Core Include chain, since a
  full ancestor tree needs an exponential number of Include paths that a
  fixed LINQ chain can't express for arbitrary depth.
- **Horse Images**: `IImageStorageService` is a new, separate abstraction from
  Person 1 Sprint 2's `IFileStorageService` (local-disk, avatar-specific) —
  horse gallery images are a distinct concern (remote storage, dimensions,
  content hashing, a deletable remote asset id). `CloudinaryImageStorageService`
  is the only class that references `CloudinaryDotNet`; validation (content
  type, file size, pixel dimensions via SixLabors.ImageSharp's header-only
  `Image.Identify`) happens before any Cloudinary API call, so invalid uploads
  never cost quota. Swapping to Azure Blob Storage later means one new class
  and one DI registration change — no caller changes anywhere in the
  Application or API layers.
- **Duplicate image detection** uses a SHA-256 content hash, checked both in
  the domain (`Horse.AddImage`) and via a unique DB index on
  `(HorseId, ContentHash)` — defense in depth, not redundant logic duplicated
  by hand.

## Modified Components

| File | Change |
|---|---|
| `Horse.cs` | Added FatherId/MotherId + Father/Mother nav; MaxImageCount/MaxLineageDepth constants; rewrote RecordOwnership to close out the prior stint; replaced the old 2-arg AddImage with a full-metadata version; added SetFather/SetMother/ClearLineage/RemoveImage/SetMainImage/ReorderImages |
| `OwnershipHistory.cs` | Now derives from SoftDeletableAuditableEntity; added SaleDate, IsActive, CloseOut, UpdateRecord, Delete |
| `HorseImage.cs` | Added StorageId/ContentType/FileSizeBytes/Width/Height/ContentHash/DisplayOrder; added SetAsMain/UnsetMain/UpdateDisplayOrder |
| `HorseConfiguration.cs` | Added Father/Mother FK config (both Restrict) |
| `OwnershipHistoryConfiguration.cs` | Added SaleDate/CreatedAt mapping, soft-delete query filter |
| `HorseImageConfiguration.cs` | Added new metadata columns; unique index on (HorseId, ContentHash) |
| `IHorseRepository.cs` / `HorseRepository.cs` | Added GetByIdWithImagesAsync, GetByIdWithParentsAsync, GetChildrenAsync, GetAncestorIdsAsync |
| `DependencyInjection.cs` (Infrastructure) | Registered IOwnershipHistoryRepository and IImageStorageService (Cloudinary); bound CloudinarySettings/ImageValidationSettings |
| `ExceptionHandlingMiddleware.cs` | Added 10 new exception → HTTP status mappings; fixed FileTooLargeException misuse on the min-size path (added FileTooSmallException) |
| `SmartHorse.Infrastructure.csproj` | Added CloudinaryDotNet, SixLabors.ImageSharp |
| `appsettings.json` | Added Cloudinary and ImageValidation sections |

No file belonging to Person 1's Authentication/User Management modules was
touched.

## Database Changes

- `Horses`: added `FatherId`, `MotherId` (both nullable Guid, self-referencing
  FK, `Restrict` delete, indexed).
- `OwnershipHistories`: added `SaleDate` (nullable), `CreatedAt`, `UpdatedAt`,
  `CreatedBy`, `UpdatedBy`, `IsDeleted`, `DeletedAt`, `DeletedBy` (from the new
  `SoftDeletableAuditableEntity` base); added a soft-delete query filter and an
  index on `(HorseId, SaleDate)`.
- `HorseImages`: added `StorageId`, `ContentType`, `FileSizeBytes`, `Width`,
  `Height`, `ContentHash`, `DisplayOrder`; added a unique index on
  `(HorseId, ContentHash)`.
- No table was renamed; no Sprint 1 column was removed.

## Migration Details

**No migration file is included.** Same environment limitation as prior
sprints (no .NET SDK here). Additionally: **the Person 2 Sprint 1 migration
was never generated either** (only Sprint 1 Person 1's `InitialCreate` exists
in the repo) — so the first migration you generate after applying this
package will capture the Horse Core schema (Sprint 1) *and* this sprint's
Ownership/Lineage/Image changes together, in one migration. That's expected
and fine; there's nothing to reconcile.

```bash
dotnet ef migrations add Person2_HorseCoreAndOwnershipLineageImages \
  --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API
dotnet ef database update \
  --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API
```

## Ownership Module

Endpoints under `/api/v1/horses/{horseId}/ownership` (current owner, history,
transfer) and `/api/v1/ownership-records/{recordId}` (update/delete a specific
historical record — not horse-scoped, since a record Id is already globally
unique). "Create Ownership" from the spec is covered by horse creation
(Sprint 1) and Transfer (this sprint) — `Horse.CurrentOwnerId` is a required
field with no "unowned" state, so a standalone create-ownership endpoint
wouldn't have a meaningful use case; documented as a scope decision, not a gap.

## Lineage Module

Endpoints under `/api/v1/horses/{horseId}/lineage`: `GET /parents`,
`GET /children`, `GET /family-tree?maxGenerations=N`, `PUT` (set father/mother),
`DELETE` (clear both). Father must be Stallion or Colt; Mother must be Mare or
Filly (Gelding explicitly excluded — a castrated male cannot sire foals).

## Image Module

Endpoints under `/api/v1/horses/{horseId}/images`: `GET` (gallery), `POST`
(upload, multipart/form-data), `PUT /{imageId}` (replace), `DELETE /{imageId}`,
`PUT /{imageId}/main`, `PUT /reorder`. Backed by Cloudinary via
`IImageStorageService`; validated for content type, min/max file size
(1 KB–5 MB default), min/max pixel dimensions (200x200–8000x8000 default), and
duplicate content (SHA-256 hash) before any remote call.

## New APIs

| Endpoint | Method | Access |
|---|---|---|
| `/api/v1/horses/{horseId}/ownership/current` | GET | Any authenticated user |
| `/api/v1/horses/{horseId}/ownership/history` | GET | Any authenticated user |
| `/api/v1/horses/{horseId}/ownership/transfer` | POST | Administrator, Owner, Veterinarian |
| `/api/v1/ownership-records/{recordId}` | PUT / DELETE | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{horseId}/lineage/parents` | GET | Any authenticated user |
| `/api/v1/horses/{horseId}/lineage/children` | GET | Any authenticated user |
| `/api/v1/horses/{horseId}/lineage/family-tree` | GET | Any authenticated user |
| `/api/v1/horses/{horseId}/lineage` | PUT / DELETE | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{horseId}/images` | GET / POST | GET: any user; POST: Administrator, Owner, Veterinarian |
| `/api/v1/horses/{horseId}/images/{imageId}` | PUT / DELETE | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{horseId}/images/{imageId}/main` | PUT | Administrator, Owner, Veterinarian |
| `/api/v1/horses/{horseId}/images/reorder` | PUT | Administrator, Owner, Veterinarian |

## Testing Results

**Not executed** — see BUILD_REPORT.md. Written: 12 new unit test cases
(Ownership: 4, Lineage: 5, Images: 4) plus 13 new integration test cases across
three controllers (Ownership: 4, Lineage: 4, Images: 4), reusing
`CustomWebApplicationFactory`. A new `FakeImageStorageService` test double
replaces the real Cloudinary implementation for integration tests, the same
way the EF Core InMemory provider replaces SQL Server — no test hits real
Cloudinary.

## Known Limitations

- **Replace Image** is implemented as upload-new-then-delete-old rather than a
  true in-place overwrite, since the upload flow always generates a unique
  Cloudinary public_id (`UniqueFilename=true`) — documented in
  `ReplaceHorseImageCommandHandler`'s doc comment.
- **SetLineageDto** treats a null FatherId/MotherId as "leave unchanged," not
  "clear it" — clearing requires the dedicated DELETE endpoint. This means one
  PUT call can't set one parent while explicitly clearing the other; a minor,
  documented API ergonomics trade-off.
- No audit-log entries are written for ownership transfers or lineage changes
  — Person 1's `AuditLog`/`AuditAction` system is scoped to auth events; adding
  horse-domain event types to it was judged out of scope for this sprint.
- Still unresolved from before (not this sprint's responsibility): the
  repository has live diagnostic/debugging code (`ChangeTrackerDiagnosticsInterceptor`,
  `SqlDiagnosticsInterceptor`) investigating a `DbUpdateConcurrencyException` on
  Login, marked `TEMPORARY`. Untouched by this sprint — flagging again since it
  logs sensitive request data and should be resolved before further release.
- I could not run `dotnet build`/`dotnet test`/`dotnet ef migrations add`
  myself (no .NET SDK, no NuGet access) — see BUILD_REPORT.md.

## Future Recommendations

1. Add a `TransferOwnershipRequestedNotification`-style event if/when the
   Notifications module (out of scope through Sprint 2) is built, so owners
   are notified of transfers.
2. Consider adding horse-domain audit events to Person 1's AuditLog system
   once it's clear that system is meant to extend beyond Auth.
3. Add a `PATCH`-style lineage endpoint if the "set one, clear the other in
   one call" ergonomics limitation becomes a real problem for the frontend.
4. Resolve and remove the unrelated diagnostic interceptors (again).
