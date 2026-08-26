using MediatR;
using SmartHorse.Application.Ownership.DTOs;

namespace SmartHorse.Application.Ownership.Commands.TransferOwnership;

public record TransferOwnershipCommand(Guid HorseId, Guid NewOwnerId, string? Notes) : IRequest<OwnershipDto>;
