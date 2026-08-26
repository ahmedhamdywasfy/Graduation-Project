using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SmartHorse.API.IntegrationTests;

/// <summary>Integration tests for Horse Images endpoints (Person 2 Sprint 2 §16), using FakeImageStorageService in place of Cloudinary.</summary>
public class HorseImagesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public HorseImagesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ApiEnvelope<T>(bool Success, T? Data, DateTime TimestampUtc);
    private record AuthResponsePayload(Guid UserId, string Email, string[] Roles, string AccessToken);
    private record HorseDtoPayload(Guid Id, string Name);
    private record HorseGalleryImageDtoPayload(Guid Id, string ImageUrl, bool IsPrimary);
    private record HorseGalleryDtoPayload(Guid HorseId, HorseGalleryImageDtoPayload[] Images);

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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/horses")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) },
            Content = JsonContent.Create(payload)
        };
        var response = await _client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HorseDtoPayload>>(JsonOptions);
        return envelope!.Data!.Id;
    }

    private static MultipartFormDataContent BuildUploadContent(string fileContent, bool isPrimary)
    {
        var bytes = Encoding.UTF8.GetBytes(fileContent);
        var content = new MultipartFormDataContent();
        var fileContentPart = new ByteArrayContent(bytes);
        fileContentPart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContentPart, "file", "photo.jpg");
        content.Add(new StringContent(isPrimary.ToString()), "isPrimary");
        return content;
    }

    [Fact]
    public async Task Upload_AsOwner_Returns201AndAddsToGallery()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/horses/{horseId}/images")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) },
            Content = BuildUploadContent("unique-image-bytes-1", isPrimary: false)
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HorseGalleryImageDtoPayload>>(JsonOptions);
        envelope!.Data!.IsPrimary.Should().BeTrue(); // first image is always primary
    }

    [Fact]
    public async Task Upload_SameImageTwice_SecondCallReturns409()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/horses/{horseId}/images")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) },
            Content = BuildUploadContent("identical-bytes", isPrimary: false)
        };
        (await _client.SendAsync(firstRequest)).StatusCode.Should().Be(HttpStatusCode.Created);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/horses/{horseId}/images")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) },
            Content = BuildUploadContent("identical-bytes", isPrimary: false)
        };
        var secondResponse = await _client.SendAsync(secondRequest);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Upload_AsBuyer_Returns403()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);
        var (_, buyerToken) = await RegisterAndGetTokenAsync("Buyer");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/horses/{horseId}/images")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", buyerToken) },
            Content = BuildUploadContent("some-bytes", isPrimary: false)
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGallery_AfterUpload_ReturnsImage()
    {
        var (ownerId, ownerToken) = await RegisterAndGetTokenAsync("Owner");
        var horseId = await CreateHorseAsync(ownerToken, ownerId);

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/horses/{horseId}/images")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) },
            Content = BuildUploadContent("gallery-test-bytes", isPrimary: false)
        };
        await _client.SendAsync(uploadRequest);

        using var galleryRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/horses/{horseId}/images")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
        };
        var galleryResponse = await _client.SendAsync(galleryRequest);

        galleryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var gallery = await galleryResponse.Content.ReadFromJsonAsync<ApiEnvelope<HorseGalleryDtoPayload>>(JsonOptions);
        gallery!.Data!.Images.Should().ContainSingle();
    }
}
