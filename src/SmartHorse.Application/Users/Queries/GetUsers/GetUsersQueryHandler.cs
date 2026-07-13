using AutoMapper;
using MediatR;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Application.Common.Models;
using SmartHorse.Application.Users.DTOs;

namespace SmartHorse.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedUserListDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<PagedUserListDto> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var criteria = new UserSearchCriteria
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            RoleFilter = request.RoleFilter,
            IsActive = request.IsActive,
            CreatedFromUtc = request.CreatedFromUtc,
            CreatedToUtc = request.CreatedToUtc,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };

        var (items, totalCount) = await _userRepository.GetPagedAsync(criteria, cancellationToken);

        return new PagedUserListDto
        {
            Items = _mapper.Map<IReadOnlyList<UserDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
