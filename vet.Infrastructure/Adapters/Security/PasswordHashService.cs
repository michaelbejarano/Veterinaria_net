using Microsoft.AspNetCore.Identity;
using vet.Application.Services;
using vet.Domain.Entities;

namespace vet.Infrastructure.Adapters.Security;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(
            new User(),
            password);
    }

    public bool VerifyPassword(
        string hashedPassword,
        string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            new User(),
            hashedPassword,
            providedPassword);

        return result != PasswordVerificationResult.Failed;
    }
}