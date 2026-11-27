using System.Text.RegularExpressions;

namespace vet.Domain.Services;

public static class EmployeeRules
{
    public static void Validate(
        string firstNames,
        string lastNames,
        string type,
        decimal monthlySalary,
        string? documentNumber,
        string? phone)
    {
        // Validar nombres y apellidos
        if (string.IsNullOrWhiteSpace(firstNames) ||
            string.IsNullOrWhiteSpace(lastNames))
        {
            throw new ArgumentException(
                "Los nombres y apellidos son obligatorios.");
        }

        // Validar tipo de trabajador
        if (type is not (
            "VETERINARIAN" or
            "GROOMING" or
            "RECEPTION" or
            "ADMIN" or
            "OTHER"))
        {
            throw new ArgumentException(
                "El tipo de empleado no es válido.");
        }

        // Validar salario
        if (monthlySalary < 0)
        {
            throw new ArgumentException(
                "El salario no puede ser negativo.");
        }

        // Validar DNI
        if (!string.IsNullOrEmpty(documentNumber) &&
            !Regex.IsMatch(documentNumber, @"\A[0-9]{8}\z"))
        {
            throw new ArgumentException(
                "El DNI debe contener exactamente 8 dígitos numéricos.");
        }

        // Validar celular
        if (!string.IsNullOrEmpty(phone) &&
            !Regex.IsMatch(phone, @"\A[0-9]{9}\z"))
        {
            throw new ArgumentException(
                "El celular debe contener exactamente 9 dígitos numéricos.");
        }
    }
}