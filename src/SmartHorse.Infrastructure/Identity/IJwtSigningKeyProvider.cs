using Microsoft.IdentityModel.Tokens;

namespace SmartHorse.Infrastructure.Identity;

/// <summary>
/// Owns the single, long-lived RSA key pair used to sign access tokens for the
/// entire lifetime of the application process.
///
/// ROOT-CAUSE CONTEXT — do not "simplify" this back to calling
/// <c>RSA.Create()</c> + <c>ImportFromPem</c> inside <see cref="JwtService"/> on
/// every call. That was the actual defect behind the
/// <c>ObjectDisposedException: Object name: 'RSA'</c> failure:
///
/// <c>Microsoft.IdentityModel.Tokens.CryptoProviderFactory.Default</c> keeps a
/// process-wide, static cache of <c>AsymmetricSignatureProvider</c> instances.
/// The cache key is <c>SecurityKey.InternalId</c>, which for an
/// <c>RsaSecurityKey</c> is a JWK thumbprint computed from the key's modulus
/// and exponent — i.e. it is derived from the VALUE of the key, not the
/// identity of the RSA object instance that happens to hold it. Every time the
/// old code did <c>RSA.Create()</c> + <c>ImportFromPem(samePrivateKey)</c> it
/// produced a brand-new RSA/RsaSecurityKey object, but one that hashed to
/// exactly the same cache slot as every previous (and every future) call,
/// because the underlying key material never changes.
///
/// The cache does not clone what it stores — it keeps a direct reference to
/// whichever RSA object instance most recently occupied that slot. Once a
/// different call's RSA object stops being referenced anywhere else, it
/// becomes eligible for finalization; the CLR is then free to release its
/// native handle. The next request that reuses that cache slot signs through
/// an <c>AsymmetricSignatureProvider</c> pointing at that now-finalized RSA
/// object, which throws <c>ObjectDisposedException</c> deep inside
/// <c>RSACng.SignHash</c>/<c>VerifyHash</c>. This is why the failure looked
/// intermittent (never on the very first call) and why "cloning" the RSA
/// immediately before use did not help — the clone still imports the same key
/// value, so it still collides in the same shared cache slot.
///
/// The correct fix is architectural: build the signing key exactly ONCE, keep
/// its backing RSA object alive for the process lifetime, and hand out the
/// SAME <see cref="SigningCredentials"/> instance for every token. This is the
/// same pattern already used (correctly) for the token *validation* key in
/// <c>SmartHorse.API.Extensions.AuthenticationExtensions</c>, which is why
/// validation never exhibited this bug — only issuance did.
/// </summary>
public interface IJwtSigningKeyProvider
{
    /// <summary>
    /// The single, process-lifetime <see cref="SigningCredentials"/> used to
    /// sign every access token. Safe to reuse concurrently across requests:
    /// signing only reads the key material, and Microsoft.IdentityModel.Tokens
    /// is explicitly designed around long-lived, reused <see cref="SecurityKey"/>
    /// instances (that is the entire reason it computes a stable InternalId for
    /// caching in the first place).
    /// </summary>
    SigningCredentials SigningCredentials { get; }
}
