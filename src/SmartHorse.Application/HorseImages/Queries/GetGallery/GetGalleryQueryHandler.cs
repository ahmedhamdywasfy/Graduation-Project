using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.HorseImages.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.HorseImages.Queries.GetGallery;

public class GetGalleryQueryHandler : IRequestHandler<GetGalleryQuery, HorseGalleryDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public GetGalleryQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<HorseGalleryDto> Handle(GetGalleryQuery request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithImagesAsync(request.HorseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.HorseId);

        return new HorseGalleryDto
        {
            HorseId = horse.Id,
            Images = _mapper.Map<IReadOnlyList<HorseGalleryImageDto>>(
                horse.Images.OrderBy(i => i.DisplayOrder).ToList())
        };
    }
}
