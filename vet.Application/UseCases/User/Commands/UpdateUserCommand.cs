
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Domain.Ports;
using RoleEntity = vet.Domain.Entities.Role;

namespace vet.Application.UseCases.User.Commands;

public class UpdateUserCommand : IRequest<bool>
{
    public long Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long RoleId { get; set; }

    public bool IsActive { get; set; }
}

public class UpdateUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar usuario existente
        var user = await unitOfWork.Users.GetByIdAsync(request.Id);

        if (user is null)
        {
            return false;
        }

        // 2. Validar correo
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            email.Length > 200)
        {
            throw new ArgumentException(
                "El correo electrónico no es válido.");
        }

        // 3. Verificar duplicados si cambia el correo
        if (!string.Equals(
                user.Email,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            var exists = await unitOfWork.Users
                .ExistsByEmailAsync(email);

            if (exists)
            {
                throw new InvalidOperationException(
                    "El correo electrónico ya está registrado.");
            }
        }

        // 4. Validar que el rol exista
        var role = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(request.RoleId);

        if (role is null)
        {
            throw new ArgumentException(
                "El rol seleccionado no existe.");
        }

        // 5. Actualizar propiedades
        user.Email = email;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;

        // 6. Guardar cambios
        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
