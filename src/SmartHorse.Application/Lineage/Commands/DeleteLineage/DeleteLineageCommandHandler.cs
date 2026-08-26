using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Lineage.Commands.DeleteLineage;

public class DeleteLineageCommandHandler : IRequestHandler<DeleteLineageCommand>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IApplicationDbContext _dbContext;

    public DeleteLineageCommandHandler(IHorseRepository horseRepository, IApplicationDbContext dbContext)
    {
        _horseRepository = horseRepository;
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteLineageCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        horse.ClearLineage();

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
