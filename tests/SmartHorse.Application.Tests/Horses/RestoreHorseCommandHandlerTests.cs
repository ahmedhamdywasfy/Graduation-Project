using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.Commands.RestoreHorse;
using SmartHorse.Application.Horses.Mappings;
using SmartHorse.Application.Tests.TestHelpers;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

using static SmartHorse.Application.Tests.TestHelpers.EntityNavigationHelper;

namespace SmartHorse.Application.Tests.Horses;

public class RestoreHorseCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly IMapper _mapper;

    public RestoreHorseCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HorseMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private RestoreHorseCommandHandler CreateHandler() => new(_horseRepository.Object, _dbContext.Object, _mapper);

    private static Horse CreateDeletedHorseWithNavigation()
    {
        var horse = new Horse(
            "Thunder", 1, 1, 1, 1, 450m, 160m, DateTime.UtcNow.AddYears(-5),
            Guid.NewGuid(), "Description", "MC-1", "REG-1");
        horse.Delete(Guid.NewGuid());

        SetNavigation(horse, nameof(Horse.Breed), new Breed("Arabian"));
        SetNavigation(horse, nameof(Horse.Color), new Color("Bay"));
        SetNavigation(horse, nameof(Horse.Gender), new Gender("Mare"));
        SetNavigation(horse, nameof(Horse.Status), new HorseStatus(HorseStatus.Names.Active));
        return horse;
    }

    [Fact]
    public async Task Handle_WithDeletedHorse_RestoresIt()
    {
        var horse = CreateDeletedHorseWithNavigation();
        _horseRepository.Setup(x => x.GetDeletedByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        _horseRepository.Setup(x => x.GetByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);

        var handler = CreateHandler();
        var result = await handler.Handle(new RestoreHorseCommand(horse.Id), CancellationToken.None);

        horse.IsDeleted.Should().BeFalse();
        horse.DeletedAt.Should().BeNull();
        result.Name.Should().Be("Thunder");
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoDeletedHorseFound_ThrowsNotFoundException()
    {
        _horseRepository.Setup(x => x.GetDeletedByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Horse?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new RestoreHorseCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
