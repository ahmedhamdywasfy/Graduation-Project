using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Application.Horses.Mappings;
using SmartHorse.Application.Horses.Queries.SearchHorses;
using SmartHorse.Domain.Entities;
using Xunit;

namespace SmartHorse.Application.Tests.Horses;

public class SearchHorsesQueryHandlerTests
{
    private readonly Mock<IHorseRepository> _horseRepository = new();
    private readonly IMapper _mapper;

    public SearchHorsesQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HorseMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private SearchHorsesQueryHandler CreateHandler() => new(_horseRepository.Object, _mapper);

    [Fact]
    public async Task Handle_PassesAllFilterCriteriaThroughToRepository()
    {
        HorseSearchCriteria? captured = null;
        _horseRepository
            .Setup(x => x.GetPagedAsync(It.IsAny<HorseSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<HorseSearchCriteria, CancellationToken>((criteria, _) => captured = criteria)
            .ReturnsAsync((new List<Horse>(), 0));

        var handler = CreateHandler();
        var query = new SearchHorsesQuery(
            Page: 2, PageSize: 10, SearchTerm: "thunder", BreedId: 1, ColorId: 2, GenderId: 3, StatusId: 4,
            MinAgeYears: 2, MaxAgeYears: 10, MinWeight: 300m, MaxWeight: 600m, MinHeight: 140m, MaxHeight: 180m,
            BirthDateFrom: new DateTime(2015, 1, 1), BirthDateTo: new DateTime(2022, 1, 1),
            SortBy: "age", SortDescending: true);

        await handler.Handle(query, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Page.Should().Be(2);
        captured.PageSize.Should().Be(10);
        captured.SearchTerm.Should().Be("thunder");
        captured.BreedId.Should().Be(1);
        captured.ColorId.Should().Be(2);
        captured.GenderId.Should().Be(3);
        captured.StatusId.Should().Be(4);
        captured.MinAgeYears.Should().Be(2);
        captured.MaxAgeYears.Should().Be(10);
        captured.MinWeight.Should().Be(300m);
        captured.MaxWeight.Should().Be(600m);
        captured.MinHeight.Should().Be(140m);
        captured.MaxHeight.Should().Be(180m);
        captured.BirthDateFrom.Should().Be(new DateTime(2015, 1, 1));
        captured.BirthDateTo.Should().Be(new DateTime(2022, 1, 1));
        captured.SortBy.Should().Be("age");
        captured.SortDescending.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoResults_ReturnsEmptyPagedList()
    {
        _horseRepository
            .Setup(x => x.GetPagedAsync(It.IsAny<HorseSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Horse>(), 0));

        var handler = CreateHandler();
        var result = await handler.Handle(new SearchHorsesQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
