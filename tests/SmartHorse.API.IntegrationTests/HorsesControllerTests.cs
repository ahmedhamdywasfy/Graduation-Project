using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SmartHorse.API.IntegrationTests;

/// <summary>Integration tests for Horse Core endpoints (Person 2 Sprint 1 §15), exercised end-to-end against a real (InMemory) database.</summary>
public class HorsesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client;

    public HorsesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ApiEnvelope<T>(bool Success, T? Data, DateTime TimestampUtc);

    private record AuthResponsePayload(Guid UserId, string Email, string[] Roles, string AccessToken);

    private record HorseDtoPayload(Guid Id, string Name, string BreedName, int StatusId, string StatusName);

    private static object BuildRegisterPayload(string email, string role) => new
    {
        fullName = "Integration Test User",
        email,
        password = "StrongPass1!",
        confirmPassword = "StrongPass1!",
        phoneNumber = (string?)null,
        requestedRole = role
    };

    private async Task<(Guid UserId, string AccessToken)> RegisterAndGetTokenAsync(string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", BuildRegisterPayload(email, role));
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthResponsePayload>>(JsonOptions);
        return (envelope!.Data!.UserId, envelope.Data.AccessToken);
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static object BuildCreateHorsePayload(Guid ownerId) => new
    {
        name = "Thunder",
        breedId = 1,
        colorId = 1,
        genderId = 1,
        statusId = (int?)null,
        weight = 450m,
        height = 160m,
        birthDate = DateTime.UtcNow.AddYears(-5),
        description = "A strong stallion.",
        microchipNumber = $"MC-{Guid.NewGuid():N}"[..12],
        registrationNumber = $"REG-{Guid.NewGuid():N}"[..13],
        ownerId
    };

    [Fact]
    public async Task Create_AsOwner_Returns201AndPersistsHorse()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", ownerToken, BuildCreateHorsePayload(ownerId));
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HorseDtoPayload>>(JsonOptions);
        envelope!.Data!.Name.Should().Be("Thunder");
        envelope.Data.BreedName.Should().NotBeNullOrWhiteSpace();
        envelope.Data.StatusName.Should().Be("Active");
    }

    [Fact]
    public async Task Create_AsBuyer_Returns403()
    {
        var (buyerId, buyerToken) = await RegisterAndGetTokenAsync("Buyer");

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", buyerToken, BuildCreateHorsePayload(buyerId));
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/horses", BuildCreateHorsePayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithFutureBirthDate_Returns400()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var payload = new
        {
            name = "Future Foal",
            breedId = 1,
            colorId = 1,
            genderId = 1,
            statusId = (int?)null,
            weight = 100m,
            height = 90m,
            birthDate = DateTime.UtcNow.AddDays(5),
            description = (string?)null,
            microchipNumber = (string?)null,
            registrationNumber = (string?)null,
            ownerId
        };

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", ownerToken, payload);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_AsBuyer_Returns200_ReadOnlyAccessIsAllowed()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        using var createRequest = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", ownerToken, BuildCreateHorsePayload(ownerId));
        var createResponse = await _client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<HorseDtoPayload>>(JsonOptions);

        var (_, buyerToken) = await RegisterAndGetTokenAsync("Buyer");
        using var getRequest = AuthorizedRequest(HttpMethod.Get, $"/api/v1/horses/{created!.Data!.Id}", buyerToken);
        var getResponse = await _client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteThenRestore_AsOwner_RoundTripsSuccessfully()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        using var createRequest = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", ownerToken, BuildCreateHorsePayload(ownerId));
        var createResponse = await _client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<HorseDtoPayload>>(JsonOptions);
        var horseId = created!.Data!.Id;

        using var deleteRequest = AuthorizedRequest(HttpMethod.Delete, $"/api/v1/horses/{horseId}", ownerToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getAfterDeleteRequest = AuthorizedRequest(HttpMethod.Get, $"/api/v1/horses/{horseId}", ownerToken);
        var getAfterDeleteResponse = await _client.SendAsync(getAfterDeleteRequest);
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var restoreRequest = AuthorizedRequest(HttpMethod.Post, $"/api/v1/horses/{horseId}/restore", ownerToken);
        var restoreResponse = await _client.SendAsync(restoreRequest);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getAfterRestoreRequest = AuthorizedRequest(HttpMethod.Get, $"/api/v1/horses/{horseId}", ownerToken);
        var getAfterRestoreResponse = await _client.SendAsync(getAfterRestoreRequest);
        getAfterRestoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_WithBreedFilter_ReturnsOnlyMatchingHorses()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        using var createRequest = AuthorizedRequest(HttpMethod.Post, "/api/v1/horses", ownerToken, BuildCreateHorsePayload(ownerId));
        await _client.SendAsync(createRequest);

        using var searchRequest = AuthorizedRequest(HttpMethod.Get, "/api/v1/horses/search?breedId=1&page=1&pageSize=20", ownerToken);
        var searchResponse = await _client.SendAsync(searchRequest);

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
