namespace vet.Domain.Services;

public static class CashSessionRules
{
    public static void ValidateClosing(
        string status,
        decimal countedCash)
    {
        if (status != "OPEN")
            throw new ArgumentException(
                "Solo se puede cerrar una caja abierta.");

        if (countedCash < 0)
            throw new ArgumentException(
                "El efectivo contado no puede ser negativo.");
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
}