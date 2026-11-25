using vet.Domain.Entities;

namespace vet.Domain.Services.Commission;

public static class CommissionRules
{
    public static void ValidateRule(
        string type,
        decimal value)
    {
        switch (type)
        {
            case "NONE":
                if (value != 0)
                    throw new ArgumentException(
                        "Una comisión de tipo NONE debe tener valor cero.");
                break;

            case "FIXED":
                if (value <= 0)
                    throw new ArgumentException(
                        "La comisión fija debe ser mayor que cero.");
                break;

            case "PERCENT":
                if (value <= 0 || value > 100)
                    throw new ArgumentException(
                        "El porcentaje debe ser mayor que cero y máximo 100.");
                break;

            default:
                throw new ArgumentException(
                    "El tipo de comisión no es válido.");
        }
    }

    public static void ValidateDates(
        DateOnly validFrom,
        DateOnly? validTo)
    {
        if (validTo.HasValue && validTo.Value < validFrom)
            throw new ArgumentException(
                "La fecha final no puede ser anterior a la fecha inicial.");
    }

    public static void ValidateOverlappingRules(
        DateOnly validFrom,
        DateOnly? validTo,
        IEnumerable<CommissionRule> existingRules)
    {
        var end = validTo ?? DateOnly.MaxValue;

        var hasConflict = existingRules.Any(rule =>
            validFrom <= (rule.ValidTo ?? DateOnly.MaxValue) &&
            rule.ValidFrom <= end);

        if (hasConflict)
            throw new ArgumentException(
                "Ya existe una regla de comisión para ese periodo.");
    }

    public static bool IsApplicable(
        DateOnly validFrom,
        DateOnly? validTo,
        DateOnly date)
    {
        return date >= validFrom &&
               (!validTo.HasValue || date <= validTo.Value);
    }

    public static decimal CalculateAmount(
        string type,
        decimal value,
        decimal calculationBase)
    {
        ValidateRule(type, value);

        if (calculationBase < 0)
            throw new ArgumentException(
                "La base de cálculo no puede ser negativa.");

        var amount = type switch
        {
            "NONE" => 0m,
            "FIXED" => value,
            "PERCENT" => calculationBase * value / 100m,

            _ => throw new ArgumentException(
                "Tipo de comisión no válido.")
        };

        return Math.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);
    }
}