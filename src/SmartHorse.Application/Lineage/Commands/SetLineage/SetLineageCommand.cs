using MediatR;
using SmartHorse.Application.Lineage.DTOs;

namespace SmartHorse.Application.Lineage.Commands.SetLineage;

/// <summary>Sets a horse's father and/or mother — Sprint 2 §4. Either field may be null to leave that parent untouched.</summary>
public record SetLineageCommand(Guid HorseId, Guid? FatherId, Guid? MotherId) : IRequest<LineageDto>;
