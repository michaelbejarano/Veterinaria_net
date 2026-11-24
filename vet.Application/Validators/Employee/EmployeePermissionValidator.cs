using vet.Application.IServices;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.Validators.Employee;

public class EmployeePermissionValidator(
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<string> ValidateManageAsync()
    {
        if (!currentUserService.IsAuthenticated ||
            currentUserService.UserId is not long userId)
        {
            throw new UnauthorizedAccessException(
                "Debe iniciar sesión.");
        }

        var currentUser = await unitOfWork.Users.GetByIdAsync(userId);

        if (currentUser is null || !currentUser.IsActive)
        {
            throw new UnauthorizedAccessException(
                "La cuenta no existe o está desactivada.");
        }

        var role = await unitOfWork.Repository<Role>()
            .GetByIdAsync(currentUser.RoleId);

        if (role is null ||
            role.Name is not ("SUPER_ADMIN" or "ADMIN"))
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para administrar empleados.");
        }

        return role.Name;
    }
}