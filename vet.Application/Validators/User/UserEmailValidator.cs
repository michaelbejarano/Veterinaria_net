using System.ComponentModel.DataAnnotations;
using vet.Domain.Ports;

namespace vet.Application.Validators.User;

public class UserEmailValidator(IUnitOfWork unitOfWork)
{
    public async Task<string> ValidateAsync(
        string? email,
        string? currentEmail = null)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !new EmailAddressAttribute().IsValid(normalizedEmail) ||
            normalizedEmail.Length > 200)
        {
            throw new ArgumentException(
                "El correo electrónico no es válido.");
        }

        // Si el correo no cambió, no buscamos duplicados.
        if (!string.IsNullOrWhiteSpace(currentEmail) &&
            string.Equals(
                currentEmail,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            return normalizedEmail;
        }

        if (await unitOfWork.Users.ExistsByEmailAsync(normalizedEmail))
        {
            throw new InvalidOperationException(
                "El correo electrónico ya está registrado.");
        }

        return normalizedEmail;
    }
}