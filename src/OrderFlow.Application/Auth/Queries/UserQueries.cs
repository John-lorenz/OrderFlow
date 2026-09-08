using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Auth.Dtos;
using OrderFlow.Application.Common;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<UserDto>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(IUserRepository users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new EntityNotFoundException("User", _currentUser.UserId);

        return new UserDto(user.Id, user.FullName, user.Email.Value, user.Role, user.IsActive, user.CreatedAtUtc, user.LastLoginAtUtc);
    }
}

public sealed record ListUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserDto>>;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _users;

    public ListUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<PagedResult<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _users.ListAsync(page, pageSize, cancellationToken);
        var dtos = items.Select(user => new UserDto(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc)).ToList();

        return new PagedResult<UserDto>(dtos, total, page, pageSize);
    }
}
