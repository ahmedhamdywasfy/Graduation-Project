using MediatR;
using SmartHorse.Application.Lineage.DTOs;

namespace SmartHorse.Application.Lineage.Queries.GetFamilyTree;

/// <summary>Sprint 2 §4 — "Get Family Tree". MaxGenerations is capped server-side at Horse.MaxLineageDepth regardless of what's requested.</summary>
public record GetFamilyTreeQuery(Guid HorseId, int MaxGenerations = 4) : IRequest<FamilyTreeNodeDto>;
