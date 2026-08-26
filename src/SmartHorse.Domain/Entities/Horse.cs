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
    /// <see cref="OwnershipHistory"/> row. Called once at creation (PreviousOwnerId
    /// null) by <c>CreateHorseCommandHandler</c>. A dedicated transfer-ownership
    /// use case (buyer/seller flow) is out of scope for this sprint — see the
    /// Implementation Report's "Future Recommendations".
    /// </summary>
    public void RecordOwnership(Guid? previousOwnerId, Guid newOwnerId, string? notes)
    {
        CurrentOwnerId = newOwnerId;
        _ownershipHistory.Add(new OwnershipHistory(Id, previousOwnerId, newOwnerId, notes));
    }

    public void AddImage(string imageUrl, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be empty.", nameof(imageUrl));
        }

        _images.Add(new HorseImage(Id, imageUrl, isPrimary));
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
