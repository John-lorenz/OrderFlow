using FluentValidation;
using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Auth.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, cancellationToken)
            ?? throw new BusinessRuleException("invalid_refresh_token", "Refresh token is invalid.");

        if (!stored.IsActive)
        {
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("invalid_refresh_token", "Refresh token is expired or revoked.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new EntityNotFoundException("User", stored.UserId);

        if (!user.IsActive)
        {
            throw new BusinessRuleException("user_inactive", "User is inactive.");
        }

        var tokens = _jwtTokenService.Generate(user.Id, user.Email.Value, user.FullName, user.Role);
        var newHash = _jwtTokenService.HashRefreshToken(tokens.RefreshToken);
        stored.Revoke(newHash);
        await _refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, newHash, tokens.RefreshTokenExpiresAtUtc, _currentUser.IpAddress),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.FullName, user.Email.Value, user.Role, tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
    }
}

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, cancellationToken);
        if (stored is null)
        {
            return;
        }

        stored.Revoke();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
