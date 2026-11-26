namespace vet.Domain.Services;

public static class CashSessionRules
{
    public static void ValidateMoney(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException(
                "El monto no puede ser negativo.");

        if (amount > 9999999999.99m)
            throw new ArgumentException(
                "El monto supera el máximo permitido.");

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException(
                "El monto admite máximo dos decimales.");
    }

    public static void ValidateClosing(
        string status,
        decimal countedCash)
    {
        if (status != "OPEN")
            throw new ArgumentException(
                "Solo se puede cerrar una caja abierta.");

        ValidateMoney(countedCash);
    }

    public static decimal CalculateExpectedCash(
        decimal openingFloat,
        IEnumerable<vet.Domain.Entities.CashMovement> movements)
    {
        var cashMovements = movements
            .Where(x => x.Method == "CASH")
            .ToList();

        var income = cashMovements
            .Where(x => x.Type == "INCOME")
            .Sum(x => x.Amount);

        var expense = cashMovements
            .Where(x => x.Type == "EXPENSE")
            .Sum(x => x.Amount);

        return openingFloat + income - expense;
    }

    public static void ValidateAvailableCash(
        decimal amount,
        decimal availableCash)
    {
        if (amount > availableCash)
            throw new ArgumentException(
                "No hay suficiente efectivo disponible en caja.");
    }
}