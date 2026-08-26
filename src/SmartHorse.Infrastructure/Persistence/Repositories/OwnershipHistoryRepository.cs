using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

public class OwnershipHistoryRepository : IOwnershipHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public OwnershipHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<OwnershipHistory?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default) =>
        _context.OwnershipHistories
            .IgnoreQueryFilters()
            .Include(o => o.PreviousOwner)
            .Include(o => o.NewOwner)
            .FirstOrDefaultAsync(o => o.Id == recordId, cancellationToken);

    public async Task<IReadOnlyList<OwnershipHistory>> GetByHorseIdAsync(Guid horseId, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = _context.OwnershipHistories.AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .Include(o => o.PreviousOwner)
            .Include(o => o.NewOwner)
            .Where(o => o.HorseId == horseId)
            .OrderByDescending(o => o.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Update(OwnershipHistory record) => _context.OwnershipHistories.Update(record);
}
