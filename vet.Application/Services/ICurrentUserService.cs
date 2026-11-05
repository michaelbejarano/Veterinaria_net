namespace vet.Application.Services;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string? Role { get; }
}