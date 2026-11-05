using MediatR;
using vet.Application.Services;
using vet.Domain.DTOs;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Auth.Commands.Login;

public class LoginCommandHandler
    : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashService passwordService,
        IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException(
                "Credenciales inválidas.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _unitOfWork.Users.GetByEmailAsync(email);

        if (user == null ||
            !user.IsActive ||
            !_passwordService.VerifyPassword(
                user.PasswordHash,
                request.Password))
        {
            throw new UnauthorizedAccessException(
                "Credenciales inválidas.");
        }

        var role = await _unitOfWork
            .Repository<vet.Domain.Entities.Role>()
            .GetByIdAsync(user.RoleId);

        if (role == null)
        {
            throw new UnauthorizedAccessException(
                "Credenciales inválidas.");
        }

        var token = _jwtTokenService.GenerateToken(
            user, role.Name);

        return new AuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            UserId = user.Id,
            Email = user.Email,
            Role = role.Name
        };
    }
}