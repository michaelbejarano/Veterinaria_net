using vet.Domain.Services.Inventory;

namespace vet.Domain.Services.Sale;

public static class SaleRules
{
    public static void ValidateSale(
        long ownerId,
        int itemCount)
    {
        if (ownerId <= 0)
            throw new ArgumentException(
                "El propietario es obligatorio.");

        if (itemCount <= 0)
            throw new ArgumentException(
                "La venta debe tener al menos un producto o servicio.");
    }

    public static void ValidatePaymentMethod(string paymentMethod)
    {
        if (paymentMethod is not
            ("CASH" or "YAPE" or "PLIN" or "CARD" or "BANK_TRANSFER"))
        {
            throw new ArgumentException(
                "Método de pago inválido.");
        }
    }

    public static void ValidateProductQuantity(
        decimal quantity,
        decimal availableStock)
    {
        InventoryRules.ValidateAvailableStock(
            quantity,
            availableStock);
    }

    public static void ValidateCashSession(string status)
    {
        if (status != "OPEN")
            throw new ArgumentException(
                "Debe existir una sesión de caja abierta.");
    }

    // Calcula el importe de un producto o servicio
    public static decimal CalculateItemTotal(
        decimal quantity,
        decimal unitPrice,
        decimal discount,
        decimal igv)
    {
        if (quantity <= 0 || unitPrice < 0)
            throw new ArgumentException(
                "Cantidad o precio inválido.");

        if (discount < 0 || discount > quantity * unitPrice)
            throw new ArgumentException(
                "Descuento inválido.");

        if (igv < 0)
            throw new ArgumentException(
                "IGV inválido.");

        return quantity * unitPrice - discount + igv;
    }

    // Calcula cuánto falta pagar después del adelanto
    public static decimal CalculateRemainingBalance(
        decimal total,
        decimal appliedDeposit)
    {
        if (total < 0 ||
            appliedDeposit < 0 ||
            appliedDeposit > total)
            throw new ArgumentException(
                "El adelanto aplicado es inválido.");

        return total - appliedDeposit;
    }
    public static void ValidateSaleConfirmation(string status)
    {
        if (status != "OPEN")
        {
            throw new ArgumentException(
                "Solo se pueden confirmar ventas abiertas.");
        }
    }

    public static void ValidateCompany(
        long saleCompanyId,
        long cashRegisterCompanyId)
    {
        if (saleCompanyId != cashRegisterCompanyId)
        {
            throw new ArgumentException(
                "La caja no pertenece a la empresa de la venta.");
        }
    }
    public static void ValidateReceiptIssuance(string status)
    {
        if (status != "CONFIRMED")
        {
            throw new ArgumentException(
                "Solo se pueden emitir recibos de ventas confirmadas.");
        }
    }
}