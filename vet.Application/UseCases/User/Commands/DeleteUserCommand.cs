using MediatR;
using vet.Application.Validators.User;
using vet.Domain.Ports;

namespace vet.Application.UseCases.User.Commands;

public class DeleteUserCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteUserCommandHandler(
    IUnitOfWork unitOfWork,
    UserPermissionValidator permissionValidator
) : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        // Comprobar autorización
        await permissionValidator.GetCurrentRoleAsync();

        // Buscar usuario
        var user = await unitOfWork.Users.GetByIdAsync(request.Id);

        if (user is null)
            return false;

        // Comprobar permisos sobre la cuenta
        await permissionValidator.ValidateManageAsync(user);

        // Desactivación lógica
        user.IsActive = false;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}