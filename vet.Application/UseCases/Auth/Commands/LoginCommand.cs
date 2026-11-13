
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.IServices;
using vet.Application.UseCases.Auth.DTOs;
using vet.Domain.Ports;
using RoleEntity = vet.Domain.Entities.Role;

namespace vet.Application.UseCases.Auth.Commands;

public class LoginCommand : IRequest<LoginResponseDto>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginCommandHandler(IUnitOfWork unitOfWork, IPasswordHashService passwordHashService, IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(request.Password))
        {
            throw new UnauthorizedAccessException(
                "Credenciales incorrectas.");
        }

        var user = await unitOfWork.Users
            .GetByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Credenciales incorrectas.");
        }

        var validPassword = passwordHashService.VerifyPassword(
            user.PasswordHash,
            request.Password);

        if (!validPassword)
        {
            throw new UnauthorizedAccessException(
                "Credenciales incorrectas.");
        }

        var role = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(user.RoleId);

        if (role is null)
        {
            throw new UnauthorizedAccessException(
                "Credenciales incorrectas.");
        }

        // Generar JWT
        var token = jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            role.Name);

        return new LoginResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = 3600
        };
    }
}
