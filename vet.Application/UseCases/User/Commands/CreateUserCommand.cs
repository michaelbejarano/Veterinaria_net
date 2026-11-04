
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using UserEntity = vet.Domain.Entities.User;
using RoleEntity = vet.Domain.Entities.Role;

namespace vet.Application.UseCases.User.Commands;

public class CreateUserCommand : IRequest<long>
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string Password { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long RoleId { get; set; }
}

public class CreateUserCommandHandler(IUnitOfWork unitOfWork, IPasswordHashService passwordHashService) : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            email.Length > 200)
        {
            throw new ArgumentException(
                "El correo electrónico no es válido.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 12)
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 12 caracteres.");
        }

        // Comprobar correo duplicado
        var exists = await unitOfWork.Users
            .ExistsByEmailAsync(email);

        if (exists)
        {
            throw new InvalidOperationException(
                "El correo ya está registrado.");
        }

        // Comprobar que el rol existe
        var role = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(request.RoleId);

        if (role is null)
        {
            throw new ArgumentException(
                "El rol seleccionado no existe.");
        }

        // Crear usuario
        var user = new UserEntity
        {
            Email = email,
            RoleId = request.RoleId,
            PasswordHash = passwordHashService.HashPassword(
                request.Password),
            IsActive = true
        };

        await unitOfWork.Users.AddAsync(user);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return user.Id;
    }
}
