using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Auth.Dtos;

public sealed record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
