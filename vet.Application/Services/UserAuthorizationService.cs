using vet.Application.IServices;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.Services;

public class UserAuthorizationService(
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public long CurrentUserId =>
        currentUserService.UserId
        ?? throw new UnauthorizedAccessException(
            "Debe iniciar sesión.");

    public async Task<string> GetCurrentRoleAsync()
    {
        if (!currentUserService.IsAuthenticated ||
            currentUserService.UserId is not long userId)
        {
            throw new UnauthorizedAccessException(
                "Debe iniciar sesión.");
        }

        var currentUser =
            await unitOfWork.Users.GetByIdAsync(userId);

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

    public void ValidateNotSelf(long targetUserId)
    {
        if (CurrentUserId == targetUserId)
        {
            throw new UnauthorizedAccessException(
                "No puede administrar su propia cuenta desde este módulo.");
        }
    }
}