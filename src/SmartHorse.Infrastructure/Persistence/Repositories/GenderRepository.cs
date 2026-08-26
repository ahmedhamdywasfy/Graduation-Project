using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

public class GenderRepository : IGenderRepository
{
    private readonly ApplicationDbContext _context;

    public GenderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Gender?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Genders.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Genders.AnyAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Gender>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Genders.AsNoTracking().OrderBy(g => g.Name).ToListAsync(cancellationToken);
}
