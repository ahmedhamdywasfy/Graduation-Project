using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHorse.Application.Horses.Commands.CreateHorse;
using SmartHorse.Application.Horses.Commands.DeleteHorse;
using SmartHorse.Application.Horses.Commands.RestoreHorse;
using SmartHorse.Application.Horses.Commands.UpdateHorse;
using SmartHorse.Application.Horses.DTOs;
using SmartHorse.Application.Horses.Queries.GetAllHorses;
using SmartHorse.Application.Horses.Queries.GetHorseById;
using SmartHorse.Application.Horses.Queries.SearchHorses;

namespace SmartHorse.API.Controllers;

/// <summary>
/// Horse Core endpoints (Person 2 Sprint 1). Read endpoints (GetById, GetAll,
/// Search) are available to any authenticated user, per this sprint's §12 —
/// "other users must have read-only permissions". Write endpoints (Create,
/// Update, Delete, Restore) require the "CanManageHorses" policy (Administrator,
/// Owner, or Veterinarian — see AuthenticationExtensions), reusing the exact
/// authentication/authorization infrastructure Person 1 already built.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/horses")]
[Authorize]
[Produces("application/json")]
public class HorsesController : ControllerBase
{
    private readonly ISender _mediator;

    public HorsesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registers a new horse. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPost]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(typeof(HorseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HorseDto>> Create(CreateHorseDto request, CancellationToken cancellationToken)
    {
        var command = new CreateHorseCommand(
            request.Name, request.BreedId, request.ColorId, request.GenderId, request.StatusId,
            request.Weight, request.Height, request.BirthDate, request.Description,
            request.MicrochipNumber, request.RegistrationNumber, request.OwnerId);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates an existing horse's details. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(typeof(HorseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HorseDto>> Update(Guid id, UpdateHorseDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateHorseCommand(
            id, request.Name, request.BreedId, request.ColorId, request.GenderId, request.StatusId,
            request.Weight, request.Height, request.BirthDate, request.Description,
            request.MicrochipNumber, request.RegistrationNumber);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Soft-deletes a horse. Administrator, Owner, or Veterinarian only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteHorseCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Restores a previously soft-deleted horse. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(typeof(HorseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HorseDto>> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RestoreHorseCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns full details for a single horse. Any authenticated user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HorseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorseDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHorseByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Unfiltered paginated horse listing. Any authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedHorseListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedHorseListDto>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllHorsesQuery(page, pageSize, sortBy, sortDescending), cancellationToken);
        return Ok(result);
    }

    /// <summary>Keyword-searchable, filterable, sortable, paginated horse listing (Person 2 Sprint 1 §6). Any authenticated user.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedHorseListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedHorseListDto>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? breedId = null,
        [FromQuery] int? colorId = null,
        [FromQuery] int? genderId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int? minAgeYears = null,
        [FromQuery] int? maxAgeYears = null,
        [FromQuery] decimal? minWeight = null,
        [FromQuery] decimal? maxWeight = null,
        [FromQuery] decimal? minHeight = null,
        [FromQuery] decimal? maxHeight = null,
        [FromQuery] DateTime? birthDateFrom = null,
        [FromQuery] DateTime? birthDateTo = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchHorsesQuery(
            page, pageSize, search, breedId, colorId, genderId, statusId,
            minAgeYears, maxAgeYears, minWeight, maxWeight, minHeight, maxHeight,
            birthDateFrom, birthDateTo, sortBy, sortDescending);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
