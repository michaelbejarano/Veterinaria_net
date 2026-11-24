namespace vet.Domain.Services;

public static class EmployeeRules
{
    public static void Validate(
        string firstNames,
        string lastNames,
        string type,
        decimal monthlySalary)
    {
        if (string.IsNullOrWhiteSpace(firstNames) ||
            string.IsNullOrWhiteSpace(lastNames))
        {
            throw new ArgumentException(
                "Los nombres y apellidos son obligatorios.");
        }

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

        if (monthlySalary < 0)
        {
            throw new ArgumentException(
                "El salario no puede ser negativo.");
        }
    }
}