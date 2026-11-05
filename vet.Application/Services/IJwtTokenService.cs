using vet.Domain.Entities;

namespace vet.Application.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user, string roleName);
}