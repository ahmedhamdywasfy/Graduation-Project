using MediatR;

namespace SmartHorse.Application.Ownership.Commands.DeleteOwnershipRecord;

/// <summary>Soft-deletes a historical ownership record (Sprint 2 §2). Never the active/current record's — see the handler for that guard.</summary>
public record DeleteOwnershipRecordCommand(Guid RecordId) : IRequest;
