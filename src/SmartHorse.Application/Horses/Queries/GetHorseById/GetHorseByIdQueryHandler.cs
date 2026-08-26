using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.DTOs;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Application.Horses.Queries.GetHorseById;

public class GetHorseByIdQueryHandler : IRequestHandler<GetHorseByIdQuery, HorseDetailsDto>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IMapper _mapper;

    public GetHorseByIdQueryHandler(IHorseRepository horseRepository, IMapper mapper)
    {
        _horseRepository = horseRepository;
        _mapper = mapper;
    }

    public async Task<HorseDetailsDto> Handle(GetHorseByIdQuery request, CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Horse), request.Id);

        return _mapper.Map<HorseDetailsDto>(horse);
    }
}
