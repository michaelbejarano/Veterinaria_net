namespace vet.Domain.Services;

public static class ProductBatchRules
{
    public static void Validate(
        string batchCode,
        DateOnly? expiryDate,
        decimal unitCost)
    {
        if (string.IsNullOrWhiteSpace(batchCode))
            throw new ArgumentException(
                "El código del lote es obligatorio.");

        if (unitCost < 0)
            throw new ArgumentException(
                "El costo unitario no puede ser negativo.");

        if (expiryDate.HasValue &&
            expiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException(
                "No se puede registrar un lote vencido.");
    }
}