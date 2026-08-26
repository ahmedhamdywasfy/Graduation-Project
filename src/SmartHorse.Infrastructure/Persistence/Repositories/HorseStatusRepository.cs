using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

public class HorseStatusRepository : IHorseStatusRepository
{
    private readonly ApplicationDbContext _context;

    public HorseStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<HorseStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.HorseStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<HorseStatus?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.HorseStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.HorseStatuses.AnyAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HorseStatus>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.HorseStatuses.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
}
