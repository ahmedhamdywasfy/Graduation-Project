using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Ownership.Commands.DeleteOwnershipRecord;

public class DeleteOwnershipRecordCommandHandler : IRequestHandler<DeleteOwnershipRecordCommand>
{
    private readonly IOwnershipHistoryRepository _ownershipHistoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public DeleteOwnershipRecordCommandHandler(
        IOwnershipHistoryRepository ownershipHistoryRepository,
        ICurrentUserService currentUser,
        IApplicationDbContext dbContext)
    {
        _ownershipHistoryRepository = ownershipHistoryRepository;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteOwnershipRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _ownershipHistoryRepository.GetByIdAsync(request.RecordId, cancellationToken)
            ?? throw new NotFoundException(nameof(OwnershipHistory), request.RecordId);

        record.Delete(_currentUser.UserId);

        _ownershipHistoryRepository.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
