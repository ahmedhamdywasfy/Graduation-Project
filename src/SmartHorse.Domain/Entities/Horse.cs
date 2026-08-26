using SmartHorse.Domain.Common;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// Core horse identity aggregate root (Person 2 Sprint 1 — Horse Core; matches
/// the Horses table already documented in the approved v0.1 §13 schema, extended
/// with the full audit/soft-delete fields this sprint requires). Reference data
/// (Breed/Color/Gender/HorseStatus) is modeled as proper FK relationships rather
/// than free text, per this sprint's Database Design §1.
///
/// As with <see cref="User"/>, business invariants that depend only on this
/// entity's own data live here as behavior methods; anything needing an external
/// service (e.g. verifying a Breed/Owner Id actually exists) stays in the
/// Application layer command handlers.
/// </summary>
public class Horse : SoftDeletableAuditableEntity
{
    public const decimal MinWeightKg = 1m;
    public const decimal MaxWeightKg = 1500m;
    public const decimal MinHeightCm = 30m;
    public const decimal MaxHeightCm = 250m;

    /// <summary>Sprint 2 §10 — "Maximum Images" validation.</summary>
    public const int MaxImageCount = 10;

    /// <summary>Sprint 2 §3 — guards against pathological/accidental ancestor-chain depth, not a real biological limit.</summary>
    public const int MaxLineageDepth = 20;

    private readonly List<HorseImage> _images = new();
    private readonly List<OwnershipHistory> _ownershipHistory = new();

    private Horse()
    {
        // Required by EF Core.
        Name = string.Empty;
    }

    public Horse(
        string name,
        int breedId,
        int colorId,
        int genderId,
        int statusId,
        decimal weight,
        decimal height,
        DateTime birthDate,
        Guid currentOwnerId,
        string? description,
        string? microchipNumber,
        string? registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Horse name is required.", nameof(name));
        }

        if (birthDate.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidHorseBirthDateException();
        }

        if (weight < MinWeightKg || weight > MaxWeightKg)
        {
            throw new InvalidHorseMeasurementException(nameof(Weight), MinWeightKg, MaxWeightKg);
        }

        if (height < MinHeightCm || height > MaxHeightCm)
        {
            throw new InvalidHorseMeasurementException(nameof(Height), MinHeightCm, MaxHeightCm);
        }

        Name = name.Trim();
        BreedId = breedId;
        ColorId = colorId;
        GenderId = genderId;
        StatusId = statusId;
        Weight = weight;
        Height = height;
        BirthDate = birthDate.Date;
        CurrentOwnerId = currentOwnerId;
        Description = description?.Trim();
        MicrochipNumber = NormalizeIdentifier(microchipNumber);
        RegistrationNumber = NormalizeIdentifier(registrationNumber);
    }

    public string Name { get; private set; }

    public int BreedId { get; private set; }
    public Breed Breed { get; private set; } = null!;

    public int ColorId { get; private set; }
    public Color Color { get; private set; } = null!;

    public int GenderId { get; private set; }
    public Gender Gender { get; private set; } = null!;

    public int StatusId { get; private set; }
    public HorseStatus Status { get; private set; } = null!;

    public decimal Weight { get; private set; }
    public decimal Height { get; private set; }
    public DateTime BirthDate { get; private set; }

    public string? Description { get; private set; }
    public string? MicrochipNumber { get; private set; }
    public string? RegistrationNumber { get; private set; }

    public Guid CurrentOwnerId { get; private set; }
    public User CurrentOwner { get; private set; } = null!;

    /// <summary>Sprint 2 §3 — Horse Lineage. Self-referencing FKs, both nullable (unknown/unset parentage is the common case).</summary>
    public Guid? FatherId { get; private set; }
    public Horse? Father { get; private set; }

    public Guid? MotherId { get; private set; }
    public Horse? Mother { get; private set; }

    public IReadOnlyCollection<HorseImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<OwnershipHistory> OwnershipHistory => _ownershipHistory.AsReadOnly();

    /// <summary>
    /// Age in whole years as of today (UTC). Deliberately NOT a persisted
    /// column (Person 2 Sprint 1 §3 — "Age must be calculated automatically") —
    /// EF Core does not map get-only computed properties by convention, so no
    /// explicit `.Ignore()` is needed in HorseConfiguration (same pattern
    /// already used by <see cref="User.IsLockedOut"/>).
    /// </summary>
    public int Age
    {
        get
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }

    public void UpdateDetails(
        string name,
        int breedId,
        int colorId,
        int genderId,
        int statusId,
        decimal weight,
        decimal height,
        DateTime birthDate,
        string? description,
        string? microchipNumber,
        string? registrationNumber,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Horse name is required.", nameof(name));
        }

        if (birthDate.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidHorseBirthDateException();
        }

        if (weight < MinWeightKg || weight > MaxWeightKg)
        {
            throw new InvalidHorseMeasurementException(nameof(Weight), MinWeightKg, MaxWeightKg);
        }

        if (height < MinHeightCm || height > MaxHeightCm)
        {
            throw new InvalidHorseMeasurementException(nameof(Height), MinHeightCm, MaxHeightCm);
        }

        Name = name.Trim();
        BreedId = breedId;
        ColorId = colorId;
        GenderId = genderId;
        StatusId = statusId;
        Weight = weight;
        Height = height;
        BirthDate = birthDate.Date;
        Description = description?.Trim();
        MicrochipNumber = NormalizeIdentifier(microchipNumber);
        RegistrationNumber = NormalizeIdentifier(registrationNumber);
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records an ownership change and appends the corresponding
    /// <see cref="OwnershipHistory"/> row, closing out the previous active
    /// record's <see cref="OwnershipHistory.SaleDate"/> in the same operation
    /// (Sprint 2 §1 — "Sale Date"). Called once at creation (previousOwnerId
    /// null, nothing to close) by <c>CreateHorseCommandHandler</c>, and again by
    /// <c>TransferOwnershipCommandHandler</c> for every subsequent transfer.
    /// </summary>
    public void RecordOwnership(Guid? previousOwnerId, Guid newOwnerId, string? notes)
    {
        var transferDate = DateTime.UtcNow;

        if (previousOwnerId.HasValue)
        {
            var activeRecord = _ownershipHistory.FirstOrDefault(o => o.IsActive)
                ?? throw new NoActiveOwnershipRecordException(Id);
            activeRecord.CloseOut(transferDate);
        }

        CurrentOwnerId = newOwnerId;
        _ownershipHistory.Add(new OwnershipHistory(Id, previousOwnerId, newOwnerId, notes));
    }

    /// <summary>Sprint 2 §3 — assigns this horse's father. Caller (Application layer) has already validated gender and circularity via IHorseRepository, since that needs a DB round trip this entity can't perform itself.</summary>
    public void SetFather(Guid fatherId)
    {
        if (fatherId == Id)
        {
            throw new SelfParentException();
        }

        FatherId = fatherId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMother(Guid motherId)
    {
        if (motherId == Id)
        {
            throw new SelfParentException();
        }

        MotherId = motherId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearLineage()
    {
        FatherId = null;
        MotherId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public HorseImage AddImage(
        string imageUrl,
        string storageId,
        string contentType,
        long fileSizeBytes,
        int width,
        int height,
        string contentHash,
        bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be empty.", nameof(imageUrl));
        }

        if (_images.Count >= MaxImageCount)
        {
            throw new MaxImagesExceededException(MaxImageCount);
        }

        if (_images.Any(i => i.ContentHash == contentHash))
        {
            throw new DuplicateHorseImageException();
        }

        // The very first image for a horse is automatically the main image.
        var shouldBePrimary = isPrimary || _images.Count == 0;

        if (shouldBePrimary)
        {
            foreach (var existing in _images)
            {
                existing.UnsetMain();
            }
        }

        var nextDisplayOrder = _images.Count == 0 ? 0 : _images.Max(i => i.DisplayOrder) + 1;
        var image = new HorseImage(Id, imageUrl, storageId, contentType, fileSizeBytes, width, height, contentHash, nextDisplayOrder, shouldBePrimary);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new NotFoundException(nameof(HorseImage), imageId);

        var wasPrimary = image.IsPrimary;
        _images.Remove(image);

        // Promote the next image (by display order) to main if the deleted one was it.
        if (wasPrimary)
        {
            var next = _images.OrderBy(i => i.DisplayOrder).FirstOrDefault();
            next?.SetAsMain();
        }
    }

    public void SetMainImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new NotFoundException(nameof(HorseImage), imageId);

        foreach (var existing in _images)
        {
            existing.UnsetMain();
        }

        image.SetAsMain();
    }

    public void ReorderImages(IReadOnlyList<Guid> orderedImageIds)
    {
        for (var i = 0; i < orderedImageIds.Count; i++)
        {
            var image = _images.FirstOrDefault(img => img.Id == orderedImageIds[i])
                ?? throw new NotFoundException(nameof(HorseImage), orderedImageIds[i]);
            image.UpdateDisplayOrder(i);
        }
    }

    /// <summary>
    /// Soft-deletes this horse (Person 2 Sprint 1 §3 — "Soft Delete must be
    /// implemented"). Guards against double-delete the same way
    /// <see cref="User.Deactivate"/> guards against double-deactivation.
    /// </summary>
    public void Delete(Guid? deletedBy)
    {
        if (IsDeleted)
        {
            throw new HorseAlreadyDeletedException(Id);
        }

        MarkDeleted(deletedBy);
    }

    public void RestoreFromDeletion()
    {
        if (!IsDeleted)
        {
            throw new HorseNotDeletedException(Id);
        }

        Restore();
    }

    private static string? NormalizeIdentifier(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
