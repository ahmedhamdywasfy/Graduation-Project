using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Ownership.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Ownership.Commands.UpdateOwnershipRecord;

/// <summary>Administrator correction of a historical ownership record's notes/dates — Sprint 2 §2 "Update Ownership".</summary>
public class UpdateOwnershipRecordCommandHandler : IRequestHandler<UpdateOwnershipRecordCommand, OwnershipHistoryRecordDto>
{
    private readonly IOwnershipHistoryRepository _ownershipHistoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateOwnershipRecordCommandHandler(
        IOwnershipHistoryRepository ownershipHistoryRepository,
        ICurrentUserService currentUser,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _ownershipHistoryRepository = ownershipHistoryRepository;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OwnershipHistoryRecordDto> Handle(UpdateOwnershipRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _ownershipHistoryRepository.GetByIdAsync(request.RecordId, cancellationToken)
            ?? throw new NotFoundException(nameof(OwnershipHistory), request.RecordId);

        record.UpdateRecord(request.Notes, request.PurchaseDate, request.SaleDate, _currentUser.UserId ?? Guid.Empty);

        _ownershipHistoryRepository.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OwnershipHistoryRecordDto>(record);
    }
}
