using MediatR;
using SmartHorse.Application.Ownership.DTOs;

namespace SmartHorse.Application.Ownership.Commands.UpdateOwnershipRecord;

public record UpdateOwnershipRecordCommand(Guid RecordId, string? Notes, DateTime PurchaseDate, DateTime? SaleDate) : IRequest<OwnershipHistoryRecordDto>;
