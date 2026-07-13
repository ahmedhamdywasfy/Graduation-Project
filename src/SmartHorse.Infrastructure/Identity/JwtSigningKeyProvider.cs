using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// Default implementation of <see cref="IJwtSigningKeyProvider"/>. Registered as
/// a Singleton (see <c>DependencyInjection.AddInfrastructure</c>): the private
/// key PEM is parsed exactly once, into exactly one <see cref="RSA"/> instance,
/// which then backs every <see cref="SigningCredentials"/> use for the rest of
/// the process's life. See the extensive comment on
/// <see cref="IJwtSigningKeyProvider"/> for why this must NOT be Scoped or
/// Transient, and must NOT re-import the PEM on every call.
/// </summary>
public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider, IDisposable
{
    /// <summary>
    /// Explicit, stable key id ("kid"). Two things depend on this being fixed
    /// and shared with the validation side
    /// (<c>SmartHorse.API.Extensions.AuthenticationExtensions</c>):
    /// 1. It makes the JWT header carry an explicit "kid", so token validation
    ///    is matched by identity rather than by implicitly probing key
    ///    material — required groundwork for rotating to a second key or
    ///    publishing a JWKS endpoint later without breaking existing tokens.
    /// 2. It gives Microsoft.IdentityModel.Tokens' internal signature-provider
    ///    cache a deterministic, explicit identity for this key instead of
    ///    relying solely on the JWK-thumbprint derivation described in
    ///    <see cref="IJwtSigningKeyProvider"/>.
    /// </summary>
    public const string KeyId = "smarthorse-rsa-1";

    private readonly RSA _rsa;

    public SigningCredentials SigningCredentials { get; }

    public JwtSigningKeyProvider(IOptions<JwtSettings> settings)
    {
        // Config values may store the PEM with literal "\n" sequences (common
        // when the key is supplied via a single-line environment variable or
        // Key Vault secret), so normalize before importing.
        var privateKeyPem = settings.Value.PrivateKeyPem.Replace("\\n", Environment.NewLine);

        _rsa = RSA.Create();
        try
        {
            _rsa.ImportFromPem(privateKeyPem);
        }
        catch
        {
            // Fail fast with the RSA disposed rather than leaking a
            // half-initialized handle if the PEM is malformed.
            _rsa.Dispose();
            throw;
        }

        var signingKey = new RsaSecurityKey(_rsa) { KeyId = KeyId };
        SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
    }

    /// <summary>
    /// Disposes the single RSA instance when the root <c>IServiceProvider</c> is
    /// disposed at application shutdown. Because this class is registered as a
    /// Singleton, the DI container guarantees this runs exactly once, after the
    /// last request has completed — never while a token could still be signed.
    /// </summary>
    public void Dispose() => _rsa.Dispose();
}
