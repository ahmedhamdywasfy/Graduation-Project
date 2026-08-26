using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IHorseRepository"/> (Person 2 Sprint 1).</summary>
public class HorseRepository : IHorseRepository
{
    private readonly ApplicationDbContext _context;

    public HorseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Horse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Horses
            .Include(h => h.Breed)
            .Include(h => h.Color)
            .Include(h => h.Gender)
            .Include(h => h.Status)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<Horse?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Horses
            .Include(h => h.Breed)
            .Include(h => h.Color)
            .Include(h => h.Gender)
            .Include(h => h.Status)
            .Include(h => h.CurrentOwner)
            .Include(h => h.Images)
            .Include(h => h.OwnershipHistory).ThenInclude(o => o.PreviousOwner)
            .Include(h => h.OwnershipHistory).ThenInclude(o => o.NewOwner)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<Horse?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Horses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == id && h.IsDeleted, cancellationToken);

    public Task<bool> MicrochipNumberExistsAsync(string microchipNumber, Guid? excludeHorseId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Horses.IgnoreQueryFilters().Where(h => h.MicrochipNumber == microchipNumber);

        if (excludeHorseId.HasValue)
        {
            query = query.Where(h => h.Id != excludeHorseId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, Guid? excludeHorseId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Horses.IgnoreQueryFilters().Where(h => h.RegistrationNumber == registrationNumber);

        if (excludeHorseId.HasValue)
        {
            query = query.Where(h => h.Id != excludeHorseId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Horse> Items, int TotalCount)> GetPagedAsync(
        HorseSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Horses
            .Include(h => h.Breed)
            .Include(h => h.Color)
            .Include(h => h.Gender)
            .Include(h => h.Status)
            .Include(h => h.Images)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(h =>
                h.Name.ToLower().Contains(term) ||
                (h.MicrochipNumber != null && h.MicrochipNumber.ToLower().Contains(term)) ||
                (h.RegistrationNumber != null && h.RegistrationNumber.ToLower().Contains(term)));
        }

        if (criteria.BreedId.HasValue)
        {
            query = query.Where(h => h.BreedId == criteria.BreedId.Value);
        }

        if (criteria.ColorId.HasValue)
        {
            query = query.Where(h => h.ColorId == criteria.ColorId.Value);
        }

        if (criteria.GenderId.HasValue)
        {
            query = query.Where(h => h.GenderId == criteria.GenderId.Value);
        }

        if (criteria.StatusId.HasValue)
        {
            query = query.Where(h => h.StatusId == criteria.StatusId.Value);
        }

        if (criteria.MinWeight.HasValue)
        {
            query = query.Where(h => h.Weight >= criteria.MinWeight.Value);
        }

        if (criteria.MaxWeight.HasValue)
        {
            query = query.Where(h => h.Weight <= criteria.MaxWeight.Value);
        }

        if (criteria.MinHeight.HasValue)
        {
            query = query.Where(h => h.Height >= criteria.MinHeight.Value);
        }

        if (criteria.MaxHeight.HasValue)
        {
            query = query.Where(h => h.Height <= criteria.MaxHeight.Value);
        }

        if (criteria.BirthDateFrom.HasValue)
        {
            query = query.Where(h => h.BirthDate >= criteria.BirthDateFrom.Value.Date);
        }

        if (criteria.BirthDateTo.HasValue)
        {
            query = query.Where(h => h.BirthDate <= criteria.BirthDateTo.Value.Date);
        }

        // Age isn't a stored column (Person 2 Sprint 1 §3 — calculated automatically),
        // so an age range filter is translated into the equivalent BirthDate range
        // here rather than in the caller. MinAgeYears (older bound on age) means an
        // EARLIER-or-equal birth date; MaxAgeYears means a LATER-or-equal birth date.
        var today = DateTime.UtcNow.Date;

        if (criteria.MinAgeYears.HasValue)
        {
            query = query.Where(h => h.BirthDate <= today.AddYears(-criteria.MinAgeYears.Value));
        }

        if (criteria.MaxAgeYears.HasValue)
        {
            query = query.Where(h => h.BirthDate >= today.AddYears(-criteria.MaxAgeYears.Value - 1));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySort(query, criteria.SortBy, criteria.SortDescending);

        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Horse> ApplySort(IQueryable<Horse> query, string sortBy, bool descending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "createdat" => descending ? query.OrderByDescending(h => h.CreatedAt) : query.OrderBy(h => h.CreatedAt),
            // Age has no stored column and increases as BirthDate gets earlier, so
            // "sort by age ascending" (youngest first) is "sort by BirthDate descending".
            "age" => descending ? query.OrderBy(h => h.BirthDate) : query.OrderByDescending(h => h.BirthDate),
            _ => descending ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name)
        };
    }

    public void Add(Horse horse) => _context.Horses.Add(horse);

    public void Update(Horse horse) => _context.Horses.Update(horse);
}
