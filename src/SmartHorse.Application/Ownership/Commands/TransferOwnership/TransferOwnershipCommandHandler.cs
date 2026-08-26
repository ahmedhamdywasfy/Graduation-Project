using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Ownership.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Ownership.Commands.TransferOwnership;

/// <summary>
/// Transfers a horse to a new owner (Sprint 2 §1–§2). Closes out the current
/// ownership stint's SaleDate and opens a new one, entirely through
/// <see cref="Horse.RecordOwnership"/> — the same domain method
/// <c>CreateHorseCommandHandler</c> uses for the very first ownership record.
/// </summary>
public class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand, OwnershipDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public TransferOwnershipCommandHandler(
        IHorseRepository horseRepository,
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _horseRepository = horseRepository;
        _userRepository = userRepository;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OwnershipDto> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithDetailsAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        if (horse.CurrentOwnerId == request.NewOwnerId)
        {
            throw new SameOwnerTransferException();
        }

        if (await _userRepository.GetByIdAsync(request.NewOwnerId, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(User), request.NewOwnerId);
        }

        horse.RecordOwnership(horse.CurrentOwnerId, request.NewOwnerId, request.Notes);

        _horseRepository.Update(horse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var refreshed = await _horseRepository.GetByIdWithDetailsAsync(horse.Id, cancellationToken);
        return _mapper.Map<OwnershipDto>(refreshed);
    }
}
