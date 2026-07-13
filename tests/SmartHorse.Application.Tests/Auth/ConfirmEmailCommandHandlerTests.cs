using FluentAssertions;
using Moq;
using SmartHorse.Application.Auth.Commands.ConfirmEmail;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

namespace SmartHorse.Application.Tests.Auth;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISecureTokenGenerator> _tokenGenerator = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();

    public ConfirmEmailCommandHandlerTests()
    {
        _requestContext.Setup(x => x.IpAddress).Returns("127.0.0.1");
        _requestContext.Setup(x => x.UserAgent).Returns("xunit-test-agent");
        _tokenGenerator.Setup(x => x.HashToken(It.IsAny<string>())).Returns((string s) => $"hash-of-{s}");
    }

    private ConfirmEmailCommandHandler CreateHandler() => new(
        _userRepository.Object, _tokenGenerator.Object, _auditLogRepository.Object, _requestContext.Object, _dbContext.Object);

    [Fact]
    public async Task Handle_WithValidToken_ConfirmsEmail()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        user.SetEmailConfirmationToken("hash-of-good-token", DateTime.UtcNow.AddHours(1));

        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        await handler.Handle(new ConfirmEmailCommand(user.Email, "good-token"), CancellationToken.None);

        user.EmailConfirmed.Should().BeTrue();
        _auditLogRepository.Verify(x => x.Add(It.Is<AuditLog>(a => a.Action == SmartHorse.Domain.Enums.AuditAction.EmailConfirmed)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsInvalidEmailConfirmationTokenException()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        user.SetEmailConfirmationToken("hash-of-expired-token", DateTime.UtcNow.AddHours(-1));

        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new ConfirmEmailCommand(user.Email, "expired-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidEmailConfirmationTokenException>();
    }

    [Fact]
    public async Task Handle_WithWrongToken_ThrowsInvalidEmailConfirmationTokenException()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        user.SetEmailConfirmationToken("hash-of-correct-token", DateTime.UtcNow.AddHours(1));

        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new ConfirmEmailCommand(user.Email, "wrong-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidEmailConfirmationTokenException>();
    }
}
