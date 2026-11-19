using MediatR;
using vet.Application.IServices;
using vet.Application.DTOs.Auth;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Auth.Querys;

public record GetCurrentUserQuery() : IRequest<CurrentUserDto?>;

public class GetCurrentUserQueryHandler(
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    public async Task<CurrentUserDto?> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated ||
            currentUserService.UserId is not long userId)
        {
            throw new UnauthorizedAccessException(
                "Debe iniciar sesión.");
        }

        var user = await unitOfWork.Users.GetByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "El usuario no existe o está desactivado.");
        }

        var role = await unitOfWork.Repository<Role>()
            .GetByIdAsync(user.RoleId);

        if (role is null)
        {
            throw new UnauthorizedAccessException(
                "El usuario no tiene un rol válido.");
        }

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = role.Name,
            IsActive = user.IsActive
        };
    }
}