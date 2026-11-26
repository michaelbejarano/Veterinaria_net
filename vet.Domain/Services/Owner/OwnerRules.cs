using System.Net.Mail;

namespace vet.Domain.Services.Owner;

public static class OwnerRules
{
    public static void Validate(
        string firstNames,
        string? documentType,
        string? documentNumber,
        string? phone,
        string? email)
    {
        if (string.IsNullOrWhiteSpace(firstNames))
            throw new ArgumentException(
                "El nombre del propietario es obligatorio.");

        ValidateLength(firstNames, 150, "Nombre");
        ValidateLength(documentType, 10, "Tipo de documento");
        ValidateLength(documentNumber, 20, "Número de documento");
        ValidateLength(phone, 30, "Teléfono");
        ValidateLength(email, 200, "Correo electrónico");

        bool hasType =
            !string.IsNullOrWhiteSpace(documentType);

        bool hasNumber =
            !string.IsNullOrWhiteSpace(documentNumber);

        if (hasType != hasNumber)
            throw new ArgumentException(
                "Debe indicar el tipo y número de documento.");

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!MailAddress.TryCreate(email.Trim(), out var address) ||
                address.Address != email.Trim())
            {
                throw new ArgumentException(
                    "El correo electrónico no es válido.");
            }
        }
    }

    private static void ValidateLength(
        string? value,
        int maxLength,
        string field)
    {
        if (value is not null &&
            value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{field} no puede superar {maxLength} caracteres.");
        }
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}