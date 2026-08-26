using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

public class BreedRepository : IBreedRepository
{
    private readonly ApplicationDbContext _context;

    public BreedRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Breed?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Breeds.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Breeds.AnyAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Breed>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Breeds.AsNoTracking().OrderBy(b => b.Name).ToListAsync(cancellationToken);
}
