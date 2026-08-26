using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHorse.Application.Lineage.Commands.DeleteLineage;
using SmartHorse.Application.Lineage.Commands.SetLineage;
using SmartHorse.Application.Lineage.DTOs;
using SmartHorse.Application.Lineage.Queries.GetChildren;
using SmartHorse.Application.Lineage.Queries.GetFamilyTree;
using SmartHorse.Application.Lineage.Queries.GetParents;

namespace SmartHorse.API.Controllers;

/// <summary>
/// Horse Lineage endpoints (Person 2 Sprint 2 §3–§4). Modify (PUT/DELETE)
/// requires the "CanManageHorses" policy; reads are available to any
/// authenticated user.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/horses/{horseId:guid}/lineage")]
[Authorize]
[Produces("application/json")]
public class LineageController : ControllerBase
{
    private readonly ISender _mediator;

    public LineageController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns this horse's immediate father/mother. Any authenticated user.</summary>
    [HttpGet("parents")]
    [ProducesResponseType(typeof(LineageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LineageDto>> GetParents(Guid horseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetParentsQuery(horseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns every horse whose father or mother is this horse. Any authenticated user.</summary>
    [HttpGet("children")]
    [ProducesResponseType(typeof(IReadOnlyList<ChildHorseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChildHorseDto>>> GetChildren(Guid horseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetChildrenQuery(horseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns the recursive ancestor tree, up to maxGenerations (default 4, hard-capped server-side). Any authenticated user.</summary>
    [HttpGet("family-tree")]
    [ProducesResponseType(typeof(FamilyTreeNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FamilyTreeNodeDto>> GetFamilyTree(
        Guid horseId, [FromQuery] int maxGenerations = 4, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFamilyTreeQuery(horseId, maxGenerations), cancellationToken);
        return Ok(result);
    }

    /// <summary>Assigns father and/or mother, with gender and circular-lineage validation. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPut]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(typeof(LineageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LineageDto>> SetLineage(Guid horseId, SetLineageDto request, CancellationToken cancellationToken)
    {
        var command = new SetLineageCommand(horseId, request.FatherId, request.MotherId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Clears both parent assignments. Administrator, Owner, or Veterinarian only.</summary>
    [HttpDelete]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLineage(Guid horseId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLineageCommand(horseId), cancellationToken);
        return NoContent();
    }
}
