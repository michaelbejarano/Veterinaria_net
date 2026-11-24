namespace vet.Application.IServices;
//Perminito que lso handlres conozcan uerid,role,IsAuthenticated
public interface ICurrentUserService
{
    long? UserId { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }
}