using AutoMapper;
using FluentAssertions;
using Moq;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Mappings;
using SmartHorse.Application.Common.Models;
using SmartHorse.Application.Users.Queries.GetUsers;
using SmartHorse.Domain.Entities;
using Xunit;

namespace SmartHorse.Application.Tests.Users;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly IMapper _mapper;

    public GetUsersQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetUsersQueryHandler CreateHandler() => new(_userRepository.Object, _mapper);

    [Fact]
    public async Task Handle_PassesCriteriaThroughToRepositoryAndMapsResults()
    {
        var user = new User("Jane Owner", "owner@example.com", "hash");
        UserSearchCriteria? capturedCriteria = null;

        _userRepository
            .Setup(x => x.GetPagedAsync(It.IsAny<UserSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<UserSearchCriteria, CancellationToken>((criteria, _) => capturedCriteria = criteria)
            .ReturnsAsync((new List<User> { user }, 1));

        var handler = CreateHandler();
        var query = new GetUsersQuery(
            Page: 2, PageSize: 10, SearchTerm: "jane", RoleFilter: "Owner",
            IsActive: true, SortBy: "email", SortDescending: true);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(u => u.Email == user.Email);

        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.Page.Should().Be(2);
        capturedCriteria.PageSize.Should().Be(10);
        capturedCriteria.SearchTerm.Should().Be("jane");
        capturedCriteria.RoleFilter.Should().Be("Owner");
        capturedCriteria.IsActive.Should().BeTrue();
        capturedCriteria.SortBy.Should().Be("email");
        capturedCriteria.SortDescending.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoResults_ReturnsEmptyPagedList()
    {
        _userRepository
            .Setup(x => x.GetPagedAsync(It.IsAny<UserSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var handler = CreateHandler();
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
