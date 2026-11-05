using MediatR;
using vet.Application.Services;
using vet.Application.UseCases.User.Commands;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Auth.Commands.CreateUser;

public class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashService _passwordService;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashService passwordService,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _currentUser = currentUser;
    }

    public async Task<long> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException(
                "Debes iniciar sesión.");

        var role = await _unitOfWork
            .Repository<Role>()
            .GetByIdAsync(request.RoleId);

        if (role == null)
            throw new ArgumentException(
                "El rol solicitado no existe.");

        var allowedRoles = _currentUser.Role switch
        {
            "SUPER_ADMIN" => new[]
            {
                "SUPER_ADMIN",
                "ADMIN",
                "VETERINARIAN",
                "RECEPTIONIST",
                "SPECIALIST"
            },

            "ADMIN" => new[]
            {
                "VETERINARIAN",
                "RECEPTIONIST",
                "SPECIALIST"
            },

            _ => Array.Empty<string>()
        };

        if (!allowedRoles.Contains(role.Name))
            throw new UnauthorizedAccessException(
                "No tienes permiso para crear usuarios con ese rol.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException(
                "El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 12)
            throw new ArgumentException(
                "La contraseña debe tener al menos 12 caracteres.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _unitOfWork.Users.ExistsByEmailAsync(email))
            throw new InvalidOperationException(
                "El correo ya está registrado.");

        var user = new Domain.Entities.User
        {
            Email = email,
            RoleId = role.Id,
            IsActive = true,
            PasswordHash = _passwordService.HashPassword(
                request.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user.Id;
    }
}