using FluentAssertions;
using Moq;
using SmartHorse.Application.Auth.Commands.ChangePassword;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

namespace SmartHorse.Application.Tests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();

    private ChangePasswordCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _auditLogRepository.Object,
        _requestContext.Object,
        _dbContext.Object);

    [Fact]
    public async Task Handle_WithCorrectCurrentPassword_UpdatesHashAndRevokesOtherSessions()
    {
        var user = new User("Jane Owner", "owner@example.com", "old-hash");
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("OldPass1!", "old-hash")).Returns(true);
        _passwordHasher.Setup(x => x.Hash("NewPass1!")).Returns("new-hash");

        var handler = CreateHandler();
        await handler.Handle(new ChangePasswordCommand(user.Id, "OldPass1!", "NewPass1!", "NewPass1!"), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        _refreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepository.Verify(x => x.Add(It.Is<AuditLog>(a => a.Action == SmartHorse.Domain.Enums.AuditAction.PasswordChanged)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ThrowsInvalidCurrentPasswordException()
    {
        var user = new User("Jane Owner", "owner@example.com", "old-hash");
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), "old-hash")).Returns(false);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new ChangePasswordCommand(user.Id, "Wrong1!", "NewPass1!", "NewPass1!"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCurrentPasswordException>();
        _refreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), "a", "NewPass1!", "NewPass1!"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
