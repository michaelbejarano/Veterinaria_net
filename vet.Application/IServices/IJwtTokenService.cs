namespace vet.Application.IServices;

public interface IJwtTokenService
{
    string GenerateToken(
        long userId,
        string email,
        string role);
}