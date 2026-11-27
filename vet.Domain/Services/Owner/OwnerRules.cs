
using System.Net.Mail;
using System.Text.RegularExpressions;

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
        {
            throw new ArgumentException(
                "Los nombres y apellidos son obligatorios.");
        }

        ValidateLength(firstNames, 150, "Nombre");
        ValidateLength(documentType, 10, "Tipo de documento");
        ValidateLength(documentNumber, 20, "Número de documento");
        ValidateLength(phone, 30, "Teléfono");
        ValidateLength(email, 200, "Correo electrónico");

        if (!Regex.IsMatch(
            firstNames.Trim(),
            @"^[\p{L}\p{M} .'-]+$"))
        {
            throw new ArgumentException(
                "El nombre contiene caracteres no permitidos.");
        }

        bool hasType =
            !string.IsNullOrWhiteSpace(documentType);

        bool hasNumber =
            !string.IsNullOrWhiteSpace(documentNumber);

        if (hasType != hasNumber)
        {
            throw new ArgumentException(
                "Debe indicar el tipo y número de documento.");
        }

        if (hasType && hasNumber)
        {
            var type = documentType!.Trim().ToUpperInvariant();
            var number = documentNumber!.Trim();

            switch (type)
            {
                case "DNI":
                    if (!Regex.IsMatch(number, @"^[0-9]{8}$"))
                    {
                        throw new ArgumentException(
                            "El DNI debe contener exactamente 8 números.");
                    }
                    break;

                case "RUC":
                    if (!Regex.IsMatch(number, @"^[0-9]{11}$"))
                    {
                        throw new ArgumentException(
                            "El RUC debe contener exactamente 11 números.");
                    }
                    break;

                case "CE":
                    if (!Regex.IsMatch(number, @"^[0-9]{9}$"))
                    {
                        throw new ArgumentException(
                            "El carné de extranjería debe contener 9 números.");
                    }
                    break;

                case "PASAPORTE":
                    if (!Regex.IsMatch(
                        number,
                        @"^[A-Za-z0-9]{6,20}$"))
                    {
                        throw new ArgumentException(
                            "El pasaporte debe tener entre 6 y 20 caracteres alfanuméricos.");
                    }
                    break;

                default:
                    throw new ArgumentException(
                        "El tipo de documento no es válido.");
            }
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            if (!Regex.IsMatch(phone.Trim(), @"^9[0-9]{8}$"))
            {
                throw new ArgumentException(
                    "El celular debe tener 9 dígitos y comenzar con 9.");
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var trimmedEmail = email.Trim();

            if (!MailAddress.TryCreate(
                    trimmedEmail,
                    out var address) ||
                address.Address != trimmedEmail ||
                !trimmedEmail.Contains('.'))
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
