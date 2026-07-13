using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// Issues RS256-signed JWT access tokens and opaque refresh token values.
///
/// Signing uses the single, process-lifetime <see cref="SigningCredentials"/>
/// exposed by <see cref="IJwtSigningKeyProvider"/> — it does NOT create or
/// import an RSA key itself. See the comment on <see cref="IJwtSigningKeyProvider"/>
/// for why building a fresh RSA/RsaSecurityKey per call (the previous
/// implementation) causes an intermittent
/// <c>ObjectDisposedException: Object name: 'RSA'</c> under
/// Microsoft.IdentityModel.Tokens' signature-provider caching.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly IJwtSigningKeyProvider _signingKeyProvider;
    private readonly ISecureTokenGenerator _secureTokenGenerator;

    public JwtService(
        IOptions<JwtSettings> settings,
        IJwtSigningKeyProvider signingKeyProvider,
        ISecureTokenGenerator secureTokenGenerator)
    {
        _settings = settings.Value;
        _signingKeyProvider = signingKeyProvider;
        _secureTokenGenerator = secureTokenGenerator;
    }

    public TimeSpan AccessTokenLifetime =>
        TimeSpan.FromMinutes(_settings.AccessTokenLifetimeMinutes);

    public TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(_settings.RefreshTokenLifetimeDays);

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: _signingKeyProvider.SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshTokenValue() =>
        _secureTokenGenerator.GenerateToken();

    public string HashRefreshToken(string refreshTokenValue) =>
        _secureTokenGenerator.HashToken(refreshTokenValue);
}
