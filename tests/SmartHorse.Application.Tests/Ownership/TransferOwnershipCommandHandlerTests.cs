using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Ownership.Commands.TransferOwnership;
using SmartHorse.Application.Ownership.Mappings;
using SmartHorse.Application.Tests.TestHelpers;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

using static SmartHorse.Application.Tests.TestHelpers.EntityNavigationHelper;

namespace SmartHorse.Application.Tests.Ownership;

public class TransferOwnershipCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly IMapper _mapper;

    private readonly User _originalOwner = new("Original Owner", "original@example.com", "hash");
    private readonly User _newOwner = new("New Owner", "new@example.com", "hash");

    public TransferOwnershipCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OwnershipMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private TransferOwnershipCommandHandler CreateHandler() => new(
        _horseRepository.Object, _userRepository.Object, _dbContext.Object, _mapper);

    private Horse CreateHorseOwnedBy(User owner)
    {
        var horse = new Horse(
            "Thunder", 1, 1, 1, 1, 450m, 160m, DateTime.UtcNow.AddYears(-5),
            owner.Id, "Description", "MC-1", "REG-1");
        horse.RecordOwnership(null, owner.Id, "Initial registration.");
        SetNavigation(horse, nameof(Horse.CurrentOwner), owner);
        return horse;
    }

    [Fact]
    public async Task Handle_WithValidNewOwner_ClosesOutPreviousStintAndOpensNew()
    {
        var horse = CreateHorseOwnedBy(_originalOwner);
        _horseRepository.Setup(x => x.GetByIdWithDetailsAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        _userRepository.Setup(x => x.GetByIdAsync(_newOwner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_newOwner);

        var handler = CreateHandler();
        await handler.Handle(new TransferOwnershipCommand(horse.Id, _newOwner.Id, "Sold at auction."), CancellationToken.None);

        horse.CurrentOwnerId.Should().Be(_newOwner.Id);
        horse.OwnershipHistory.Should().HaveCount(2);
        horse.OwnershipHistory.Should().Contain(h => h.NewOwnerId == _originalOwner.Id && h.SaleDate != null);
        horse.OwnershipHistory.Should().Contain(h => h.NewOwnerId == _newOwner.Id && h.SaleDate == null);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TransferringToCurrentOwner_ThrowsSameOwnerTransferException()
    {
        var horse = CreateHorseOwnedBy(_originalOwner);
        _horseRepository.Setup(x => x.GetByIdWithDetailsAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new TransferOwnershipCommand(horse.Id, _originalOwner.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<SameOwnerTransferException>();
    }

    [Fact]
    public async Task Handle_WithUnknownNewOwner_ThrowsNotFoundException()
    {
        var horse = CreateHorseOwnedBy(_originalOwner);
        _horseRepository.Setup(x => x.GetByIdWithDetailsAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        _userRepository.Setup(x => x.GetByIdAsync(_newOwner.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new TransferOwnershipCommand(horse.Id, _newOwner.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenHorseNotFound_ThrowsNotFoundException()
    {
        _horseRepository.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Horse?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new TransferOwnershipCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
