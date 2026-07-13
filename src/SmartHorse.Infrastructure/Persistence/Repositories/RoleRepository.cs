using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default) =>
        await _context.Roles.Where(r => names.Contains(r.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);
}
