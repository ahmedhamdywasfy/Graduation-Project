using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHorse.Application.Ownership.Commands.DeleteOwnershipRecord;
using SmartHorse.Application.Ownership.Commands.TransferOwnership;
using SmartHorse.Application.Ownership.Commands.UpdateOwnershipRecord;
using SmartHorse.Application.Ownership.DTOs;
using SmartHorse.Application.Ownership.Queries.GetCurrentOwner;
using SmartHorse.Application.Ownership.Queries.GetOwnershipHistory;

namespace SmartHorse.API.Controllers;

/// <summary>
/// Ownership Module endpoints (Person 2 Sprint 2 §1–§2). Transfer/Update/Delete
/// require the "CanManageHorses" policy (Administrator, Owner, Veterinarian —
/// same as HorsesController's write endpoints, per Sprint 2 §13); reads are
/// available to any authenticated user.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/horses/{horseId:guid}/ownership")]
[Authorize]
[Produces("application/json")]
public class OwnershipController : ControllerBase
{
    private readonly ISender _mediator;

    public OwnershipController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the current owner and purchase date. Any authenticated user.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(OwnershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnershipDto>> GetCurrentOwner(Guid horseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentOwnerQuery(horseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns the full ownership timeline (Purchase/Sale Date per stint). Any authenticated user.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<OwnershipHistoryRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OwnershipHistoryRecordDto>>> GetHistory(Guid horseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOwnershipHistoryQuery(horseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Transfers the horse to a new owner. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPost("transfer")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(typeof(OwnershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnershipDto>> Transfer(Guid horseId, TransferOwnershipDto request, CancellationToken cancellationToken)
    {
        var command = new TransferOwnershipCommand(horseId, request.NewOwnerId, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

/// <summary>
/// Direct historical-record corrections (Person 2 Sprint 2 §2 "Update Ownership" /
/// "Delete Ownership Record"). Not horse-scoped in its route because a record's
/// own Id is already globally unique and sufficient — matches how audit-log-style
/// records are addressed elsewhere in this API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/ownership-records")]
[Authorize(Policy = "CanManageHorses")]
[Produces("application/json")]
public class OwnershipRecordsController : ControllerBase
{
    private readonly ISender _mediator;

    public OwnershipRecordsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Corrects a historical ownership record's notes/dates. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPut("{recordId:guid}")]
    [ProducesResponseType(typeof(OwnershipHistoryRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnershipHistoryRecordDto>> Update(Guid recordId, UpdateOwnershipRecordDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateOwnershipRecordCommand(recordId, request.Notes, request.PurchaseDate, request.SaleDate);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Soft-deletes a historical ownership record. Administrator, Owner, or Veterinarian only.</summary>
    [HttpDelete("{recordId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid recordId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteOwnershipRecordCommand(recordId), cancellationToken);
        return NoContent();
    }
}
