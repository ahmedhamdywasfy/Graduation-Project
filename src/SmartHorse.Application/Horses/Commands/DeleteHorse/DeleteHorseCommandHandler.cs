using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Horses.Commands.DeleteHorse;

public class DeleteHorseCommandHandler : IRequestHandler<DeleteHorseCommand>
{
    private readonly IHorseRepository _horseRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public DeleteHorseCommandHandler(
        IHorseRepository horseRepository,
        ICurrentUserService currentUser,
        IApplicationDbContext dbContext)
    {
        _horseRepository = horseRepository;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteHorseCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.Id);

        horse.Delete(_currentUser.UserId);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
