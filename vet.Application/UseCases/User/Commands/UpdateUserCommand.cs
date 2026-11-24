
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
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

public class UpdateUserCommandHandler(
    IUnitOfWork unitOfWork,
    UserEmailValidator emailValidator,
    UserAuthorizationService authorizationService
) : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Verificar autorización.
        var currentRole =
            await authorizationService.GetCurrentRoleAsync();

        var user = await unitOfWork.Users.GetByIdAsync(request.Id);

        if (user is null)
            return false;

        // No permitir modificar la propia cuenta.
        authorizationService.ValidateNotSelf(user.Id);

        // Comprobar el rol actual del usuario.
        var existingRole = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(user.RoleId);

        if (existingRole is null)
            throw new ArgumentException(
                "El usuario tiene un rol inválido.");

        // Comprobar si podemos administrar esta cuenta.
        UserRoleRules.ValidateManageRole(
            currentRole,
            existingRole.Name);

        // Verificar el nuevo rol.
        var newRole = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(request.RoleId);

        if (newRole is null)
            throw new ArgumentException(
                "El rol seleccionado no existe.");

        // Comprobar si podemos asignar ese rol.
        UserRoleRules.ValidateRoleAssignment(
            currentRole,
            newRole.Name);

        // Validar correo.
        var email = emailValidator.Validate(request.Email);

        if (!string.Equals(
                user.Email,
                email,
                StringComparison.OrdinalIgnoreCase) &&
            await unitOfWork.Users.ExistsByEmailAsync(email))
        {
            throw new InvalidOperationException(
                "El correo electrónico ya está registrado.");
        }

        // Actualizar usuario.
        user.Email = email;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
