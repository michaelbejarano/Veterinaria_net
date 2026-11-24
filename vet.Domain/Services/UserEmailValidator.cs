using System.ComponentModel.DataAnnotations;

namespace vet.Domain.Services;

public class UserEmailValidator
{
    public string Validate(string? email)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            normalizedEmail.Length > 200 ||
            !new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            throw new ArgumentException(
                "El correo electrónico no es válido.");
        }

        return normalizedEmail;
    }
}