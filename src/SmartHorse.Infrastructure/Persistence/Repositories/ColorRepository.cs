using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

public class ColorRepository : IColorRepository
{
    private readonly ApplicationDbContext _context;

    public ColorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Color?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Colors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Colors.AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Color>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Colors.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);
}
