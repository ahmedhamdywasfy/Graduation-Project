using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHorse.Application.HorseImages.Commands.DeleteHorseImage;
using SmartHorse.Application.HorseImages.Commands.ReorderHorseImages;
using SmartHorse.Application.HorseImages.Commands.ReplaceHorseImage;
using SmartHorse.Application.HorseImages.Commands.SetMainHorseImage;
using SmartHorse.Application.HorseImages.Commands.UploadHorseImage;
using SmartHorse.Application.HorseImages.DTOs;
using SmartHorse.Application.HorseImages.Queries.GetGallery;

namespace SmartHorse.API.Controllers;

/// <summary>
/// Horse Images endpoints (Person 2 Sprint 2 §5, §8). Upload/Delete/reorder/
/// set-main require the "CanManageHorses" policy; reads are available to any
/// authenticated user. Files are stored remotely via Cloudinary behind
/// <c>IImageStorageService</c> — see Infrastructure/Images.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/horses/{horseId:guid}/images")]
[Authorize]
[Produces("application/json")]
public class HorseImagesController : ControllerBase
{
    // Mirrors ImageValidationSettings.MaxFileSizeBytes default — kept in sync
    // manually since [RequestSizeLimit] needs a compile-time constant, the same
    // trade-off already accepted for avatar upload in Person 1 Sprint 2.
    private const long MaxImageUploadBytes = 5 * 1024 * 1024;

    private readonly ISender _mediator;

    public HorseImagesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the horse's full image gallery, ordered by display order. Any authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HorseGalleryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorseGalleryDto>> GetGallery(Guid horseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGalleryQuery(horseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Uploads a new gallery image (multipart/form-data). Validated for content
    /// type, size, dimensions, and duplicates (Sprint 2 §6) before ever reaching
    /// Cloudinary. Administrator, Owner, or Veterinarian only.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanManageHorses")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    [ProducesResponseType(typeof(HorseGalleryImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HorseGalleryImageDto>> Upload(
        Guid horseId, IFormFile file, [FromForm] bool isPrimary, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadHorseImageCommand(horseId, stream, file.FileName, file.ContentType, isPrimary);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetGallery), new { horseId }, result);
    }

    /// <summary>Replaces an existing image's file while preserving its position and main/primary flag. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPut("{imageId:guid}")]
    [Authorize(Policy = "CanManageHorses")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    [ProducesResponseType(typeof(HorseGalleryImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HorseGalleryImageDto>> Replace(Guid horseId, Guid imageId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new ReplaceHorseImageCommand(horseId, imageId, stream, file.FileName, file.ContentType);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Deletes an image from the gallery and remote storage. Administrator, Owner, or Veterinarian only.</summary>
    [HttpDelete("{imageId:guid}")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid horseId, Guid imageId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteHorseImageCommand(horseId, imageId), cancellationToken);
        return NoContent();
    }

    /// <summary>Sets an existing image as the horse's main/primary image. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPut("{imageId:guid}/main")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMain(Guid horseId, Guid imageId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetMainHorseImageCommand(horseId, imageId), cancellationToken);
        return NoContent();
    }

    /// <summary>Reorders the gallery given a full ordered list of image Ids. Administrator, Owner, or Veterinarian only.</summary>
    [HttpPut("reorder")]
    [Authorize(Policy = "CanManageHorses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(Guid horseId, ReorderHorseImagesDto request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderHorseImagesCommand(horseId, request.OrderedImageIds), cancellationToken);
        return NoContent();
    }
}
