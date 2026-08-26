using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Horses.Commands.DeleteHorse;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

namespace SmartHorse.Application.Tests.Horses;

public class DeleteHorseCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();

    private DeleteHorseCommandHandler CreateHandler() => new(_horseRepository.Object, _currentUser.Object, _dbContext.Object);

    private static Horse CreateHorse() => new(
        "Thunder", 1, 1, 1, 1, 450m, 160m, DateTime.UtcNow.AddYears(-5),
        Guid.NewGuid(), "Description", "MC-1", "REG-1");

    [Fact]
    public async Task Handle_WithExistingHorse_SoftDeletesIt()
    {
        var horse = CreateHorse();
        var deletedByUserId = Guid.NewGuid();
        _horseRepository.Setup(x => x.GetByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        _currentUser.Setup(x => x.UserId).Returns(deletedByUserId);

        var handler = CreateHandler();
        await handler.Handle(new DeleteHorseCommand(horse.Id), CancellationToken.None);

        horse.IsDeleted.Should().BeTrue();
        horse.DeletedBy.Should().Be(deletedByUserId);
        horse.DeletedAt.Should().NotBeNull();
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHorseNotFound_ThrowsNotFoundException()
    {
        _horseRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Horse?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new DeleteHorseCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyDeleted_ThrowsHorseAlreadyDeletedException()
    {
        var horse = CreateHorse();
        horse.Delete(Guid.NewGuid());
        _horseRepository.Setup(x => x.GetByIdAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new DeleteHorseCommand(horse.Id), CancellationToken.None);

        await act.Should().ThrowAsync<HorseAlreadyDeletedException>();
    }
}
