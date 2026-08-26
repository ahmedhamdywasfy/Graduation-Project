using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.Commands.CreateHorse;
using SmartHorse.Application.Horses.Mappings;
using SmartHorse.Application.Tests.TestHelpers;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

using static SmartHorse.Application.Tests.TestHelpers.EntityNavigationHelper;

namespace SmartHorse.Application.Tests.Horses;

public class CreateHorseCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<IBreedRepository> _breedRepository = new();
    private readonly Mock<IColorRepository> _colorRepository = new();
    private readonly Mock<IGenderRepository> _genderRepository = new();
    private readonly Mock<IHorseStatusRepository> _horseStatusRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly IMapper _mapper;

    private static readonly Guid OwnerId = Guid.NewGuid();
    private readonly Breed _breed = new("Arabian");
    private readonly Color _color = new("Bay");
    private readonly Gender _gender = new("Mare");
    private readonly HorseStatus _activeStatus = new(HorseStatus.Names.Active);
    private readonly User _owner = new("Jane Owner", "owner@example.com", "hash");

    public CreateHorseCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HorseMappingProfile>());
        _mapper = config.CreateMapper();

        _breedRepository.Setup(x => x.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _colorRepository.Setup(x => x.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _genderRepository.Setup(x => x.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _horseStatusRepository.Setup(x => x.GetByNameAsync(HorseStatus.Names.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_activeStatus);
        _userRepository.Setup(x => x.GetByIdAsync(OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(_owner);
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Horse construction only stores FK Ids (by design — see Horse.cs); real
        // usage relies on EF Core's query materialization to populate navigation
        // properties. For this in-memory test, wire them via reflection onto the
        // private setters so the AutoMapper profile (which reads e.g. Breed.Name)
        // has something valid to read, exactly like a real query would provide.
        _horseRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _createdHorse);
        _horseRepository.Setup(x => x.Add(It.IsAny<Horse>())).Callback<Horse>(h =>
        {
            SetNavigation(h, nameof(Horse.Breed), _breed);
            SetNavigation(h, nameof(Horse.Color), _color);
            SetNavigation(h, nameof(Horse.Gender), _gender);
            SetNavigation(h, nameof(Horse.Status), _activeStatus);
            SetNavigation(h, nameof(Horse.CurrentOwner), _owner);
            _createdHorse = h;
        });
    }

    private Horse? _createdHorse;

    private CreateHorseCommandHandler CreateHandler() => new(
        _horseRepository.Object, _breedRepository.Object, _colorRepository.Object, _genderRepository.Object,
        _horseStatusRepository.Object, _userRepository.Object, _currentUser.Object, _dbContext.Object, _mapper);

    private static CreateHorseCommand ValidCommand() => new(
        "Thunder", BreedId: 1, ColorId: 1, GenderId: 1, StatusId: null,
        Weight: 450m, Height: 160m, BirthDate: DateTime.UtcNow.AddYears(-5),
        Description: "A strong stallion.", MicrochipNumber: "MC-001", RegistrationNumber: "REG-001", OwnerId: OwnerId);

    [Fact]
    public async Task Handle_WithValidData_CreatesHorseAndRecordsInitialOwnership()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Thunder");
        result.BreedName.Should().Be("Arabian");

        _createdHorse.Should().NotBeNull();
        _createdHorse!.OwnershipHistory.Should().ContainSingle(o => o.NewOwnerId == OwnerId && o.PreviousOwnerId == null);
        _createdHorse.CurrentOwnerId.Should().Be(OwnerId);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownBreed_ThrowsNotFoundException()
    {
        _breedRepository.Setup(x => x.ExistsAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var command = ValidCommand() with { BreedId = 999 };
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithDuplicateMicrochipNumber_ThrowsDuplicateMicrochipNumberException()
    {
        _horseRepository.Setup(x => x.MicrochipNumberExistsAsync("MC-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateMicrochipNumberException>();
    }

    [Fact]
    public async Task Handle_WithDuplicateRegistrationNumber_ThrowsDuplicateRegistrationNumberException()
    {
        _horseRepository.Setup(x => x.RegistrationNumberExistsAsync("REG-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateRegistrationNumberException>();
    }

    [Fact]
    public async Task Handle_WithUnknownOwner_ThrowsNotFoundException()
    {
        _userRepository.Setup(x => x.GetByIdAsync(OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithoutExplicitStatus_ResolvesActiveStatusByName()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.StatusName.Should().Be(HorseStatus.Names.Active);
        _horseStatusRepository.Verify(x => x.GetByNameAsync(HorseStatus.Names.Active, It.IsAny<CancellationToken>()), Times.Once);
    }
}
