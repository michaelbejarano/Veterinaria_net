namespace vet.Domain.Services;

public static class InventoryRules
{
    public static void ValidateEntry(
        decimal quantity,
        decimal unitCost)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.");

        if (unitCost < 0)
            throw new ArgumentException(
                "El costo unitario no puede ser negativo.");
    }

    public static decimal CalculateStock(
        IEnumerable<(string Type, decimal Quantity)> movements)
    {
        return movements.Sum(m => m.Type switch
        {
            "IN" or "ADJUSTMENT_IN" => m.Quantity,
            "OUT" or "ADJUSTMENT_OUT" => -m.Quantity,
            _ => 0m
        });
    }
}