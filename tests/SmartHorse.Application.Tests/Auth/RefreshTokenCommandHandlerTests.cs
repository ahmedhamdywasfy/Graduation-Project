using FluentAssertions;
using Moq;
using SmartHorse.Application.Auth.Commands.RefreshToken;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

namespace SmartHorse.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();

    public RefreshTokenCommandHandlerTests()
    {
        _requestContext.Setup(x => x.IpAddress).Returns("127.0.0.1");
        _requestContext.Setup(x => x.UserAgent).Returns("xunit-test-agent");
        _jwtService.Setup(x => x.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        _jwtService.Setup(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));
        _jwtService.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns((string s) => $"hash-of-{s}");
        _jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).Returns("new-access-token");
        _jwtService.Setup(x => x.GenerateRefreshTokenValue()).Returns("new-raw-refresh-token");
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _refreshTokenRepository.Object,
        _userRepository.Object,
        _jwtService.Object,
        _auditLogRepository.Object,
        _requestContext.Object,
        _dbContext.Object);

    [Fact]
    public async Task Handle_WithValidToken_RotatesAndReturnsNewPair()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        var role = new Role(Role.Names.Owner);
        user.AssignRole(role);
        var existingToken = user.IssueRefreshToken("hash-of-old-token", DateTime.UtcNow.AddDays(1));

        _refreshTokenRepository.Setup(x => x.GetByTokenHashAsync("hash-of-old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(x => x.GetByIdWithRolesAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-refresh-token");
        existingToken.IsRevoked.Should().BeTrue();
        existingToken.ReplacedByTokenId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyRotated_DetectsReuseAndRevokesAllSessions()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        var oldToken = user.IssueRefreshToken("hash-of-old-token", DateTime.UtcNow.AddDays(1));
        var newToken = user.IssueRefreshToken("hash-of-new-token", DateTime.UtcNow.AddDays(1));
        oldToken.MarkReplacedBy(newToken.Id); // simulate: already rotated once

        _refreshTokenRepository.Setup(x => x.GetByTokenHashAsync("hash-of-old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
        _refreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepository.Verify(x => x.Add(It.Is<AuditLog>(a => a.Action == SmartHorse.Domain.Enums.AuditAction.RefreshTokenReuseDetected)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsInvalidRefreshTokenException()
    {
        _refreshTokenRepository.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsInvalidRefreshTokenException()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        var expiredToken = user.IssueRefreshToken("hash-of-expired-token", DateTime.UtcNow.AddMinutes(-1));

        _refreshTokenRepository.Setup(x => x.GetByTokenHashAsync("hash-of-expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }
}
