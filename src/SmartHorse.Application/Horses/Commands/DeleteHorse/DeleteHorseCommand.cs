using MediatR;

namespace SmartHorse.Application.Horses.Commands.DeleteHorse;

/// <summary>Soft delete (Person 2 Sprint 1 §3, §5).</summary>
public record DeleteHorseCommand(Guid Id) : IRequest;
