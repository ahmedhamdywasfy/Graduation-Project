using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SmartHorse.API.IntegrationTests;

/// <summary>Integration tests for Ownership Module endpoints (Person 2 Sprint 2 §16).</summary>
public class OwnershipControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public OwnershipControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ApiEnvelope<T>(bool Success, T? Data, DateTime TimestampUtc);
    private record AuthResponsePayload(Guid UserId, string Email, string[] Roles, string AccessToken);
    private record HorseDtoPayload(Guid Id, string Name);
    private record OwnershipDtoPayload(Guid HorseId, Guid OwnerId, string OwnerName, DateTime PurchaseDate);

    private static object BuildRegisterPayload(string email, string role) => new
    {
        fullName = "Integration Test User", email, password = "StrongPass1!", confirmPassword = "StrongPass1!",
        phoneNumber = (string?)null, requestedRole = role
    };

    private async Task<(Guid UserId, string AccessToken)> RegisterAndGetTokenAsync(string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email, role));
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponsePayload>>(JsonOptions);
        return (envelope!.Data!.UserId, envelope.Data.AccessToken);
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url) { Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) } };
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private async Task<Guid> CreateHorseAsync(string ownerToken, Guid ownerId)
    {
        var payload = new
        {
            name = "Thunder", breedId = 1, colorId = 1, genderId = 1, statusId = (int?)null,
            weight = 450m, height = 160m, birthDate = DateTime.UtcNow.AddYears(-5),
            description = (string?)null,
            microchipNumber = $"MC-{Guid.NewGuid():N}"[..12],
            registrationNumber = $"REG-{Guid.NewGuid():N}"[..13],
            ownerId
        };

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", ownerToken, payload);
        var response = await _client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HorseDtoPayload>>(JsonOptions);
        return envelope!.Data!.Id;
    }

    [Fact]
    public async Task Transfer_AsOwner_UpdatesCurrentOwnerAndHistory()
    {
        var (originalOwnerId, originalOwnerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(originalOwnerToken, originalOwnerId);

        var (newOwnerId, _) = await RegisterAndGetTokenAsync("Owner");

        using var transferRequest = AuthorizedRequest(
            HttpMethod.Post, $"/api/v1/horses/{horseId}/ownership/transfer", originalOwnerToken,
            new { newOwnerId, notes = "Sold at auction." });
        var transferResponse = await _client.SendAsync(transferRequest);

        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transferred = await transferResponse.Content.ReadFromJsonAsync<ApiEnvelope<OwnershipDtoPayload>>(JsonOptions);
        transferred!.Data!.OwnerId.Should().Be(newOwnerId);

        using var historyRequest = AuthorizedRequest(HttpMethod.Get, $"/api/v1/horses/{horseId}/ownership/history", originalOwnerToken);
        var historyResponse = await _client.SendAsync(historyRequest);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Transfer_ToSameOwner_Returns409()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);

        using var request = AuthorizedRequest(
            HttpMethod.Post, $"/api/v1/horses/{horseId}/ownership/transfer", ownerToken,
            new { newOwnerId = ownerId, notes = (string?)null });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Transfer_AsBuyer_Returns403()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);
        var (buyerId, buyerToken) = await RegisterAndGetTokenAsync("Buyer");

        using var request = AuthorizedRequest(
            HttpMethod.Post, $"/api/v1/horses/{horseId}/ownership/transfer", buyerToken,
            new { newOwnerId = buyerId, notes = (string?)null });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCurrentOwner_AsReadOnlyUser_Returns200()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);
        var (_, buyerToken) = await RegisterAndGetTokenAsync("Buyer");

        using var request = AuthorizedRequest(HttpMethod.Get, $"/api/v1/horses/{horseId}/ownership/current", buyerToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
