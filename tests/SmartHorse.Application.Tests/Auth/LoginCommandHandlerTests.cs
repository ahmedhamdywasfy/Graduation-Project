using FluentAssertions;
using Moq;
using SmartHorse.Application.Auth.Commands.Login;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

namespace SmartHorse.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();

    private LoginCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _jwtService.Object,
        _auditLogRepository.Object,
        _requestContext.Object,
        _dbContext.Object);

    private static User CreateUser(string email = "owner@example.com", string passwordHash = "hashed")
    {
        var user = new User("Jane Owner", email, passwordHash);
        var role = new Role(Role.Names.Owner);
        user.AssignRole(role);
        return user;
    }

    public LoginCommandHandlerTests()
    {
        _requestContext.Setup(x => x.IpAddress).Returns("127.0.0.1");
        _requestContext.Setup(x => x.UserAgent).Returns("xunit-test-agent");
        _jwtService.Setup(x => x.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        _jwtService.Setup(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));
        _jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).Returns("access-token");
        _jwtService.Setup(x => x.GenerateRefreshTokenValue()).Returns("raw-refresh-token");
        _jwtService.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns("hashed-refresh-token");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResponseAndRecordsLoginAudit()
    {
        var user = CreateUser();
        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("Correct123!", user.PasswordHash)).Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new LoginCommand(user.Email, "Correct123!"), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        result.Roles.Should().Contain(Role.Names.Owner);

        _auditLogRepository.Verify(x => x.Add(It.Is<AuditLog>(a => a.Action == SmartHorse.Domain.Enums.AuditAction.Login)), Times.Once);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsInvalidCredentialsExceptionAndLogsFailure()
    {
        _userRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommand("nobody@example.com", "whatever"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _auditLogRepository.Verify(x => x.Add(It.Is<AuditLog>(a => a.Action == SmartHorse.Domain.Enums.AuditAction.LoginFailed)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_RecordsFailedAttemptAndThrows()
    {
        var user = CreateUser();
        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommand(user.Email, "WrongPassword1!"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_AfterMaxFailedAttempts_LocksAccountAndThrowsOnNextAttempt()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RecordFailedLogin();
        }

        user.IsLockedOut.Should().BeTrue();

        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommand(user.Email, "Whatever1!"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task Handle_WhenAccountInactive_ThrowsAccountInactiveException()
    {
        var user = CreateUser();
        user.Deactivate();

        _userRepository.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new LoginCommand(user.Email, "Whatever1!"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountInactiveException>();
    }
}
