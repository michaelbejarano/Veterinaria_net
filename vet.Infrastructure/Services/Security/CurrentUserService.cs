using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using vet.Application.IServices;

namespace vet.Infrastructure.Services.Security;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor
) : ICurrentUserService
{
    private ClaimsPrincipal? User =>
        httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public long? UserId
    {
        get
        {
            var userId = User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return long.TryParse(userId, out var id)
                ? id
                : null;
        }
    }

    public string? Role =>
        User?.FindFirstValue(ClaimTypes.Role);
}