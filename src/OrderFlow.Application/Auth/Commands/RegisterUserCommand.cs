using FluentValidation;
using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Auth.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Auth.Commands;

public sealed record RegisterUserCommand(string FullName, string Email, string Password, UserRole? Role)
    : IRequest<AuthResponse>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
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

    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var role = request.Role ?? UserRole.Operator;
        if (role != UserRole.Operator && _currentUser.Role != UserRole.Admin)
        {
            throw new BusinessRuleException("forbidden_role", "Only administrators can create Manager or Admin accounts.");
        }

        var user = User.Create(request.FullName, request.Email, _passwordHasher.Hash(request.Password), role);
        await _users.AddAsync(user, cancellationToken);

        var tokens = _jwtTokenService.Generate(user.Id, user.Email.Value, user.FullName, user.Role);
        await _refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, _jwtTokenService.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAtUtc, _currentUser.IpAddress),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.FullName, user.Email.Value, user.Role, tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
    }
}
