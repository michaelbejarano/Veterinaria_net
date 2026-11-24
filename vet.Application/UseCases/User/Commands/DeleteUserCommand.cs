using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
using RoleEntity = vet.Domain.Entities.Role;

namespace vet.Application.UseCases.User.Commands;

public class DeleteUserCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteUserCommandHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        // Comprobar autorización.
        var currentRole =
            await authorizationService.GetCurrentRoleAsync();

        var user = await unitOfWork.Users.GetByIdAsync(request.Id);

        if (user is null)
            return false;

        // Impedir la autoadministración.
        authorizationService.ValidateNotSelf(user.Id);

        // Obtener el rol de la cuenta.
        var targetRole = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(user.RoleId);

        if (targetRole is null)
            throw new ArgumentException(
                "El usuario tiene un rol inválido.");

        // Aplicar las reglas de dominio.
        UserRoleRules.ValidateManageRole(
            currentRole,
            targetRole.Name);

        // Desactivación lógica.
        user.IsActive = false;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}