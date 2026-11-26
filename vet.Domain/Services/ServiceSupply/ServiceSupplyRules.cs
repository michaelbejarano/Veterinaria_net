namespace vet.Domain.Services.ServiceSupply;

public static class ServiceSupplyRules
{
    public static void ValidateIdentifiers(
        long serviceId,
        long productId)
    {
        if (serviceId <= 0)
            throw new ArgumentException(
                "El identificador del servicio no es válido.");

        if (productId <= 0)
            throw new ArgumentException(
                "El identificador del producto no es válido.");
    }

    public static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.");

        if (quantity > 99999999999.999m)
            throw new ArgumentException(
                "La cantidad supera el máximo permitido.");

        if (decimal.Round(quantity, 3) != quantity)
            throw new ArgumentException(
                "La cantidad admite máximo tres decimales.");
    }

    public static void ValidateActiveEntities(
        bool serviceIsActive,
        bool productIsActive)
    {
        if (!serviceIsActive)
            throw new ArgumentException(
                "El servicio está inactivo.");

        if (!productIsActive)
            throw new ArgumentException(
                "El producto está inactivo.");
    }
}