
using vet.Application.IServices;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.Validators.User;

public class UserPermissionValidator(
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<string> GetCurrentRoleAsync()
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

        if (role is null)
        {
            throw new UnauthorizedAccessException(
                "El usuario no tiene un rol válido.");
        }

        if (role.Name is not ("SUPER_ADMIN" or "ADMIN"))
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para administrar usuarios.");
        }

        return role.Name;
    }

    public async Task ValidateCreateAsync(long targetRoleId)
    {
        var currentRole = await GetCurrentRoleAsync();

        var targetRole = await unitOfWork.Repository<Role>()
            .GetByIdAsync(targetRoleId);

        if (targetRole is null)
        {
            throw new ArgumentException(
                "El rol seleccionado no existe.");
        }

        if (currentRole == "SUPER_ADMIN")
            return;

        if (currentRole == "ADMIN" &&
            targetRole.Name is
                ("VETERINARIAN" or "RECEPTIONIST" or "SPECIALIST"))
        {
            return;
        }

        throw new UnauthorizedAccessException(
            "No tiene permisos para asignar este rol.");
    }

    public async Task ValidateManageAsync(
        Domain.Entities.User targetUser,
        long? newRoleId = null)
    {
        var currentRole = await GetCurrentRoleAsync();

        if (currentUserService.UserId == targetUser.Id)
        {
            throw new UnauthorizedAccessException(
                "No puede administrar su propia cuenta desde este módulo.");
        }

        var targetRole = await unitOfWork.Repository<Role>()
            .GetByIdAsync(targetUser.RoleId);

        if (targetRole is null)
        {
            throw new ArgumentException(
                "El usuario tiene un rol inválido.");
        }

        if (currentRole == "ADMIN" &&
            targetRole.Name is ("SUPER_ADMIN" or "ADMIN"))
        {
            throw new UnauthorizedAccessException(
                "No puede modificar una cuenta administrativa.");
        }

        if (newRoleId.HasValue)
        {
            await ValidateCreateAsync(newRoleId.Value);
        }
    }
}
