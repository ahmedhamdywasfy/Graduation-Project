using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Lineage.Commands.SetLineage;
using SmartHorse.Application.Lineage.Mappings;
using SmartHorse.Application.Tests.TestHelpers;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

using static SmartHorse.Application.Tests.TestHelpers.EntityNavigationHelper;

namespace SmartHorse.Application.Tests.Lineage;

public class SetLineageCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly IMapper _mapper;

    public SetLineageCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LineageMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private SetLineageCommandHandler CreateHandler() => new(_horseRepository.Object, _dbContext.Object, _mapper);

    private static Horse CreateHorseWithGender(string genderName)
    {
        var horse = new Horse("Test Horse", 1, 1, 1, 1, 450m, 160m, DateTime.UtcNow.AddYears(-5), Guid.NewGuid(), null, null, null);
        SetNavigation(horse, nameof(Horse.Breed), new Breed("Arabian"));
        SetNavigation(horse, nameof(Horse.Gender), new Gender(genderName));
        return horse;
    }

    [Fact]
    public async Task Handle_WithValidStallionFather_AssignsFather()
    {
        var son = CreateHorseWithGender("Colt");
        var father = CreateHorseWithGender("Stallion");

        _horseRepository.Setup(x => x.GetByIdAsync(son.Id, It.IsAny<CancellationToken>())).ReturnsAsync(son);
        _horseRepository.Setup(x => x.GetByIdAsync(father.Id, It.IsAny<CancellationToken>())).ReturnsAsync(father);
        _horseRepository.Setup(x => x.GetAncestorIdsAsync(father.Id, Horse.MaxLineageDepth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());
        _horseRepository.Setup(x => x.GetByIdWithParentsAsync(son.Id, It.IsAny<CancellationToken>())).ReturnsAsync(son);

        var handler = CreateHandler();
        await handler.Handle(new SetLineageCommand(son.Id, father.Id, null), CancellationToken.None);

        son.FatherId.Should().Be(father.Id);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithGeldingAsFather_ThrowsInvalidParentGenderException()
    {
        var son = CreateHorseWithGender("Colt");
        var gelding = CreateHorseWithGender("Gelding");

        _horseRepository.Setup(x => x.GetByIdAsync(son.Id, It.IsAny<CancellationToken>())).ReturnsAsync(son);
        _horseRepository.Setup(x => x.GetByIdAsync(gelding.Id, It.IsAny<CancellationToken>())).ReturnsAsync(gelding);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new SetLineageCommand(son.Id, gelding.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidParentGenderException>();
    }

    [Fact]
    public async Task Handle_WithMareAsFather_ThrowsInvalidParentGenderException()
    {
        var son = CreateHorseWithGender("Colt");
        var mare = CreateHorseWithGender("Mare");

        _horseRepository.Setup(x => x.GetByIdAsync(son.Id, It.IsAny<CancellationToken>())).ReturnsAsync(son);
        _horseRepository.Setup(x => x.GetByIdAsync(mare.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mare);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new SetLineageCommand(son.Id, mare.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidParentGenderException>();
    }

    [Fact]
    public async Task Handle_WhenCandidateParentsAncestorsIncludeTheHorseItself_ThrowsCircularLineageException()
    {
        var horseA = CreateHorseWithGender("Mare");
        var horseB = CreateHorseWithGender("Stallion");

        // horseA is already an ancestor of horseB (e.g. B's grandsire is A) —
        // assigning B as A's father would close a loop.
        _horseRepository.Setup(x => x.GetByIdAsync(horseA.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horseA);
        _horseRepository.Setup(x => x.GetByIdAsync(horseB.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horseB);
        _horseRepository.Setup(x => x.GetAncestorIdsAsync(horseB.Id, Horse.MaxLineageDepth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { horseA.Id });

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new SetLineageCommand(horseA.Id, horseB.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<CircularLineageException>();
    }

    [Fact]
    public async Task Handle_AssigningHorseAsItsOwnFather_ThrowsSelfParentException()
    {
        var horse = CreateHorseWithGender("Stallion");

        _horseRepository.Setup(x => x.GetByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        _horseRepository.Setup(x => x.GetAncestorIdsAsync(horse.Id, Horse.MaxLineageDepth, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new SetLineageCommand(horse.Id, horse.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<SelfParentException>();
    }
}
