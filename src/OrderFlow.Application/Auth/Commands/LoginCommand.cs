using FluentValidation;
using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Auth.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new BusinessRuleException("invalid_credentials", "Invalid email or password.");

        if (!user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleException("invalid_credentials", "Invalid email or password.");
        }

        user.RegisterLogin();
        var tokens = _jwtTokenService.Generate(user.Id, user.Email.Value, user.FullName, user.Role);
        await _refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, _jwtTokenService.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAtUtc, _currentUser.IpAddress),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.FullName, user.Email.Value, user.Role, tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
    }
}
