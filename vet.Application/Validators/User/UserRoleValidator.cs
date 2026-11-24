using vet.Domain.Ports;
using RoleEntity = vet.Domain.Entities.Role;

namespace vet.Application.Validators.User;

public class UserRoleValidator(IUnitOfWork unitOfWork)
{
    public async Task ValidateAsync(long roleId)
    {
        var role = await unitOfWork
            .Repository<RoleEntity>()
            .GetByIdAsync(roleId);

        if (role is null)
        {
            throw new ArgumentException(
                "El rol seleccionado no existe.");
        }
    }
}