using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
}
