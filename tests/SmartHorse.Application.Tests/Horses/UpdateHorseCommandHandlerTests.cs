using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.Commands.UpdateHorse;
using SmartHorse.Application.Horses.Mappings;
using SmartHorse.Application.Tests.TestHelpers;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

using static SmartHorse.Application.Tests.TestHelpers.EntityNavigationHelper;

namespace SmartHorse.Application.Tests.Horses;

public class UpdateHorseCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<IBreedRepository> _breedRepository = new();
    private readonly Mock<IColorRepository> _colorRepository = new();
    private readonly Mock<IGenderRepository> _genderRepository = new();
    private readonly Mock<IHorseStatusRepository> _horseStatusRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly IMapper _mapper;

    private readonly Breed _breed = new("Thoroughbred");
    private readonly Color _color = new("Chestnut");
    private readonly Gender _gender = new("Gelding");
    private readonly HorseStatus _status = new(HorseStatus.Names.ForSale);
    private readonly User _owner = new("Jane Owner", "owner@example.com", "hash");

    public UpdateHorseCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HorseMappingProfile>());
        _mapper = config.CreateMapper();

        _breedRepository.Setup(x => x.ExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _colorRepository.Setup(x => x.ExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _genderRepository.Setup(x => x.ExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _horseStatusRepository.Setup(x => x.ExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
    }

    private Horse CreateExistingHorse()
    {
        var horse = new Horse(
            "Old Name", breedId: 1, colorId: 1, genderId: 1, statusId: 1,
            weight: 400m, height: 150m, birthDate: DateTime.UtcNow.AddYears(-3),
            currentOwnerId: _owner.Id, description: "Original", microchipNumber: "MC-100", registrationNumber: "REG-100");

        SetNavigation(horse, nameof(Horse.Breed), _breed);
        SetNavigation(horse, nameof(Horse.Color), _color);
        SetNavigation(horse, nameof(Horse.Gender), _gender);
        SetNavigation(horse, nameof(Horse.Status), _status);
        SetNavigation(horse, nameof(Horse.CurrentOwner), _owner);
        return horse;
    }

    private UpdateHorseCommandHandler CreateHandler() => new(
        _horseRepository.Object, _breedRepository.Object, _colorRepository.Object, _genderRepository.Object,
        _horseStatusRepository.Object, _currentUser.Object, _dbContext.Object, _mapper);

    [Fact]
    public async Task Handle_WithValidData_UpdatesHorseDetails()
    {
        var horse = CreateExistingHorse();
        _horseRepository.Setup(x => x.GetByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);

        var command = new UpdateHorseCommand(
            horse.Id, "New Name", BreedId: 2, ColorId: 2, GenderId: 2, StatusId: 2,
            Weight: 500m, Height: 165m, BirthDate: DateTime.UtcNow.AddYears(-6),
            Description: "Updated", MicrochipNumber: "MC-100", RegistrationNumber: "REG-100");

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("New Name");
        horse.Weight.Should().Be(500m);
        horse.StatusId.Should().Be(2);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHorseNotFound_ThrowsNotFoundException()
    {
        _horseRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Horse?)null);

        var handler = CreateHandler();
        var command = new UpdateHorseCommand(
            Guid.NewGuid(), "Name", 2, 2, 2, 2, 500m, 165m, DateTime.UtcNow.AddYears(-6), null, null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithMicrochipBelongingToAnotherHorse_ThrowsDuplicateMicrochipNumberException()
    {
        var horse = CreateExistingHorse();
        _horseRepository.Setup(x => x.GetByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        _horseRepository.Setup(x => x.MicrochipNumberExistsAsync("MC-999", horse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateHorseCommand(
            horse.Id, "New Name", 2, 2, 2, 2, 500m, 165m, DateTime.UtcNow.AddYears(-6), null, "MC-999", null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateMicrochipNumberException>();
    }
}
