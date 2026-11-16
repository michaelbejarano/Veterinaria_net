using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Domain.Ports;
using vet.Application.Validators.User;

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
    UserRoleValidator roleValidator,
    UserPermissionValidator permissionValidator
) : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Comprobar autorización incluso si el ID no existe
        await permissionValidator.GetCurrentRoleAsync();

        // Buscar usuario
        var user = await unitOfWork.Users.GetByIdAsync(request.Id);

        if (user is null)
            return false;

        // Comprobar permisos sobre el usuario y nuevo rol
        await permissionValidator.ValidateManageAsync(
            user,
            request.RoleId);

        // Validaciones existentes
        var email = await emailValidator.ValidateAsync(
            request.Email,
            user.Email);

        await roleValidator.ValidateAsync(request.RoleId);

        // Actualizar usuario
        user.Email = email;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}