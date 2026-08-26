namespace SmartHorse.Application.Lineage.DTOs;

/// <summary>
/// A single node in a family tree (Sprint 2 §11 "Get Family Tree") — recursive:
/// each node carries its own Father/Mother sub-nodes up to the query's requested
/// depth (capped at <see cref="SmartHorse.Domain.Entities.Horse.MaxLineageDepth"/>).
/// </summary>
public class FamilyTreeNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BreedName { get; set; } = string.Empty;
    public string GenderName { get; set; } = string.Empty;
    public int Generation { get; set; }

    public FamilyTreeNodeDto? Father { get; set; }
    public FamilyTreeNodeDto? Mother { get; set; }
}
