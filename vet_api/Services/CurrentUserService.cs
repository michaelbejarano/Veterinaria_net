using System.Security.Claims;
using vet.Application.Services;

namespace vet_api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?
            .User.Identity?.IsAuthenticated == true;

    public string? Role =>
        _httpContextAccessor.HttpContext?
            .User.FindFirst(ClaimTypes.Role)?.Value;
}