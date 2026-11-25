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
                        "Una regla sin comisión debe tener valor cero.");
                break;

            case "FIXED":
                if (value <= 0)
                    throw new ArgumentException(
                        "La comisión fija debe ser mayor que cero.");
                break;

            case "PERCENT":
                if (value <= 0 || value > 100)
                    throw new ArgumentException(
                        "El porcentaje debe ser mayor que cero y menor o igual a 100.");
                break;

            default:
                throw new ArgumentException(
                    "Tipo de comisión inválido.");
        }
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

        decimal amount = type switch
        {
            "NONE" => 0m,
            "FIXED" => value,
            "PERCENT" => calculationBase * value / 100m,

            _ => throw new ArgumentException(
                "Tipo de comisión inválido.")
        };

        return Math.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);
    }

    public static bool IsApplicable(
        DateOnly validFrom,
        DateOnly? validTo,
        DateOnly date)
    {
        return validFrom <= date &&
               (!validTo.HasValue || validTo.Value >= date);
    }
}