using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace SmartHorse.API.IntegrationTests;

/// <summary>Integration tests for authentication endpoints (Sprint 2 §14), exercised end-to-end against a real (InMemory) database.</summary>
public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ApiEnvelope<T>(bool Success, T? Data, DateTime TimestampUtc);

    private record AuthResponsePayload(
        Guid UserId, string FullName, string Email, string[] Roles,
        string AccessToken, DateTime AccessTokenExpiresAtUtc,
        string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

    private static object BuildRegisterPayload(string email) => new
    {
        fullName = "Integration Test Owner",
        email,
        password = "StrongPass1!",
        confirmPassword = "StrongPass1!",
        phoneNumber = (string?)null,
        requestedRole = "Owner"
    };

    [Fact]
    public async Task Register_WithValidData_Returns200AndAuthResponse()
    {
        var email = $"register-{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponsePayload>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Email.Should().Be(email.ToLowerInvariant());
        envelope.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        envelope.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        envelope.Data.Roles.Should().Contain("Owner");
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400WithValidationErrors()
    {
        var payload = new
        {
            fullName = "Weak Password User",
            email = $"weak-{Guid.NewGuid():N}@example.com",
            password = "weak",
            confirmPassword = "weak",
            phoneNumber = (string?)null,
            requestedRole = "Owner"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndTokens()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "StrongPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponsePayload>>(JsonOptions);
        envelope!.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"login-fail-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassword1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_WithValidToken_Returns200AndOwnProfile()
    {
        var email = $"profile-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email));
        var registerEnvelope = await registerResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponsePayload>>(JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerEnvelope!.Data!.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_AsNonAdministrator_Returns403()
    {
        var email = $"nonadmin-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email));
        var registerEnvelope = await registerResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponsePayload>>(JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerEnvelope!.Data!.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
