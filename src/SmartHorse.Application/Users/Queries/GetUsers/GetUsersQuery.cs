using MediatR;
using SmartHorse.Application.Users.DTOs;

namespace SmartHorse.Application.Users.Queries.GetUsers;

/// <summary>
/// Administrator-only paginated, searchable, sortable, filterable user listing
/// (Sprint 2 §7 — User Search; backs v0.2 §2.3 GET /admin/users).
/// </summary>
public record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? RoleFilter = null,
    bool? IsActive = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null,
    string SortBy = "fullName",
    bool SortDescending = false) : IRequest<PagedUserListDto>;
