using MediatR;

namespace SmartHorse.Application.Lineage.Commands.DeleteLineage;

/// <summary>Clears both parent assignments for a horse — Sprint 2 §4 "Delete" lineage.</summary>
public record DeleteLineageCommand(Guid HorseId) : IRequest;
