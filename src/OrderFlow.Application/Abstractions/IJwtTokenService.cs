using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Abstractions;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, DateTime RefreshTokenExpiresAtUtc);

public interface IJwtTokenService
{
    TokenPair Generate(Guid userId, string email, string fullName, UserRole role);
    string HashRefreshToken(string refreshToken);
}
