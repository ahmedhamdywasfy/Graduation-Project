using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SmartHorse.API.IntegrationTests;

/// <summary>Integration tests for Horse Lineage endpoints (Person 2 Sprint 2 §16).</summary>
public class LineageControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public LineageControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ApiEnvelope<T>(bool Success, T? Data, DateTime TimestampUtc);
    private record AuthResponsePayload(Guid UserId, string Email, string[] Roles, string AccessToken);
    private record HorseDtoPayload(Guid Id, string Name);
    private record LineageDtoPayload(Guid HorseId, Guid? FatherId, string? FatherName, Guid? MotherId, string? MotherName);

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

    private async Task<Guid> CreateHorseAsync(string ownerToken, Guid ownerId, int genderId)
    {
        var payload = new
        {
            name = $"Horse-{Guid.NewGuid():N}"[..15], breedId = 1, colorId = 1, genderId, statusId = (int?)null,
            weight = 450m, height = 160m, birthDate = DateTime.UtcNow.AddYears(-8),
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
    public async Task SetLineage_WithValidStallionFather_AssignsFather()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var stallionId = await CreateHorseAsync(ownerToken, ownerId, genderId: 1); // seeded gender order: Stallion=1
        var foalId = await CreateHorseAsync(ownerToken, ownerId, genderId: 4);     // Colt=4

        using var request = AuthorizedRequest(
            HttpMethod.Put, $"/api/v1/horses/{foalId}/lineage", ownerToken,
            new { fatherId = stallionId, motherId = (Guid?)null });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiEnvelope<LineageDtoPayload>>(JsonOptions);
        result!.Data!.FatherId.Should().Be(stallionId);
    }

    [Fact]
    public async Task SetLineage_WithMareAsFather_Returns400()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var mareId = await CreateHorseAsync(ownerToken, ownerId, genderId: 2); // Mare=2
        var foalId = await CreateHorseAsync(ownerToken, ownerId, genderId: 4);

        using var request = AuthorizedRequest(
            HttpMethod.Put, $"/api/v1/horses/{foalId}/lineage", ownerToken,
            new { fatherId = mareId, motherId = (Guid?)null });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetLineage_CreatingACircularChain_Returns409()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var grandsireId = await CreateHorseAsync(ownerToken, ownerId, genderId: 1);
        var sireId = await CreateHorseAsync(ownerToken, ownerId, genderId: 1);

        // sire's father = grandsire
        using var firstRequest = AuthorizedRequest(
            HttpMethod.Put, $"/api/v1/horses/{sireId}/lineage", ownerToken,
            new { fatherId = grandsireId, motherId = (Guid?)null });
        (await _client.SendAsync(firstRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        // now attempt grandsire's father = sire -> would close a loop (sire -> grandsire -> sire)
        using var circularRequest = AuthorizedRequest(
            HttpMethod.Put, $"/api/v1/horses/{grandsireId}/lineage", ownerToken,
            new { fatherId = sireId, motherId = (Guid?)null });
        var circularResponse = await _client.SendAsync(circularRequest);

        circularResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetFamilyTree_ReturnsRootNode()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId, genderId: 1);

        using var request = AuthorizedRequest(HttpMethod.Get, $"/api/v1/horses/{horseId}/lineage/family-tree", ownerToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
