using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Application.HorseImages.Commands.UploadHorseImage;
using SmartHorse.Application.HorseImages.Mappings;
using SmartHorse.Application.Tests.TestHelpers;
using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Exceptions;
using Xunit;

using static SmartHorse.Application.Tests.TestHelpers.EntityNavigationHelper;

namespace SmartHorse.Application.Tests.HorseImages;

public class UploadHorseImageCommandHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly Mock<IImageStorageService> _imageStorageService = new();
    private readonly Mock<IApplicationDbContext> _dbContext = new();
    private readonly IMapper _mapper;

    public UploadHorseImageCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HorseImageMappingProfile>());
        _mapper = config.CreateMapper();

        _imageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageUploadResult("https://cdn.example.com/img.jpg", "storage-id-1", "image/jpeg", 2048, 800, 600));
    }

    private UploadHorseImageCommandHandler CreateHandler() => new(
        _horseRepository.Object, _imageStorageService.Object, _dbContext.Object, _mapper);

    private static Horse CreateHorse() => new(
        "Thunder", 1, 1, 1, 1, 450m, 160m, DateTime.UtcNow.AddYears(-5),
        Guid.NewGuid(), "Description", "MC-1", "REG-1");

    private static (Stream Stream, string HashHex) BuildContent(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        return (new MemoryStream(bytes), hash);
    }

    [Fact]
    public async Task Handle_WithValidImage_AddsToGalleryAsPrimaryWhenFirst()
    {
        var horse = CreateHorse();
        _horseRepository.Setup(x => x.GetByIdWithImagesAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        var (stream, _) = BuildContent("first-image-bytes");

        var handler = CreateHandler();
        var result = await handler.Handle(new UploadHorseImageCommand(horse.Id, stream, "photo.jpg", "image/jpeg", false), CancellationToken.None);

        result.IsPrimary.Should().BeTrue(); // first image is always primary regardless of the requested flag
        horse.Images.Should().ContainSingle();
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateContentHash_ThrowsDuplicateHorseImageException()
    {
        var horse = CreateHorse();
        var (stream, hashHex) = BuildContent("duplicate-bytes");

        // Simulate an existing image with the same content hash already on the horse.
        horse.AddImage("https://cdn.example.com/existing.jpg", "storage-existing", "image/jpeg", 1024, 400, 400, hashHex, isPrimary: true);

        _horseRepository.Setup(x => x.GetByIdWithImagesAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new UploadHorseImageCommand(horse.Id, stream, "photo.jpg", "image/jpeg", false), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateHorseImageException>();
        _imageStorageService.Verify(x => x.UploadAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGalleryAtMaxCapacity_ThrowsMaxImagesExceededException()
    {
        var horse = CreateHorse();
        for (var i = 0; i < Horse.MaxImageCount; i++)
        {
            horse.AddImage($"https://cdn.example.com/img{i}.jpg", $"storage-{i}", "image/jpeg", 1024, 400, 400, $"hash-{i}", isPrimary: i == 0);
        }

        _horseRepository.Setup(x => x.GetByIdWithImagesAsync(horse.Id, It.IsAny<CancellationToken>())).ReturnsAsync(horse);
        var (stream, _) = BuildContent("one-too-many");

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new UploadHorseImageCommand(horse.Id, stream, "photo.jpg", "image/jpeg", false), CancellationToken.None);

        await act.Should().ThrowAsync<MaxImagesExceededException>();
    }

    [Fact]
    public async Task Handle_WhenHorseNotFound_ThrowsNotFoundException()
    {
        _horseRepository.Setup(x => x.GetByIdWithImagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Horse?)null);
        var (stream, _) = BuildContent("whatever");

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new UploadHorseImageCommand(Guid.NewGuid(), stream, "photo.jpg", "image/jpeg", false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
