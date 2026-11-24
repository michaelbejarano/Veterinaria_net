namespace vet.Domain.Services;

public static class CashMovementRules
{
    public static void ValidateManualMovement(
        string sessionStatus,
        string type,
        string category,
        string method,
        decimal amount)
    {
        if (sessionStatus != "OPEN")
            throw new ArgumentException(
                "La caja debe estar abierta para registrar movimientos.");

        if (type != "INCOME" && type != "EXPENSE")
            throw new ArgumentException(
                "El tipo debe ser INCOME o EXPENSE.");

        if (category != "DEPOSIT" &&
            category != "EXPENSE" &&
            category != "WITHDRAWAL" &&
            category != "ADJUSTMENT")
            throw new ArgumentException(
                "Categoría no permitida para movimientos manuales.");

        if (type == "INCOME" &&
            category != "DEPOSIT" &&
            category != "ADJUSTMENT")
            throw new ArgumentException(
                "Categoría inválida para un ingreso manual.");

        if (type == "EXPENSE" &&
            category != "EXPENSE" &&
            category != "WITHDRAWAL" &&
            category != "ADJUSTMENT")
            throw new ArgumentException(
                "Categoría inválida para un egreso manual.");

        if (method != "CASH")
            throw new ArgumentException(
                "Los movimientos manuales deben ser en efectivo.");

        if (amount <= 0)
            throw new ArgumentException(
                "El monto debe ser mayor que cero.");
    }
}