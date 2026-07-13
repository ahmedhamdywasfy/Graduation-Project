using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IUserRepository"/> (v0.1 Section 11 — Repository Pattern).</summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        UserSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(criteria.RoleFilter))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == criteria.RoleFilter));
        }

        if (criteria.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == criteria.IsActive.Value);
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= criteria.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySort(query, criteria.SortBy, criteria.SortDescending);

        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<User> ApplySort(IQueryable<User> query, string sortBy, bool descending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "createdat" => descending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => descending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName)
        };
    }

    public void Add(User user) => _context.Users.Add(user);

    public void Update(User user) => _context.Users.Update(user);
}
