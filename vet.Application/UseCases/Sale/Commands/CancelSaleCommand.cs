
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Sale;

using SaleItemEntity = vet.Domain.Entities.SaleItem;
using PaymentEntity = vet.Domain.Entities.Payment;
using DepositEntity = vet.Domain.Entities.DepositApplication;
using InvoiceEntity = vet.Domain.Entities.Invoice;
using MovementEntity = vet.Domain.Entities.InventoryMovement;

namespace vet.Application.UseCases.Sale.Commands;

public class CancelSaleCommand : IRequest<bool>
{
    public long SaleId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class CancelSaleCommandHandler
    : IRequestHandler<CancelSaleCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelSaleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        CancelSaleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar solicitud

        if (request.SaleId <= 0)
            throw new ArgumentException(
                "La venta es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException(
                "Debe indicar el motivo de anulación.");

        var reason = request.Reason.Trim();

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 2. Bloquear venta

                var sale = await _unitOfWork
                    .GetSaleForUpdateAsync(
                        request.SaleId,
                        ct);

                if (sale is null)
                    throw new KeyNotFoundException(
                        "La venta no existe.");

                // 3. Validar estado

                SaleRules.ValidateSaleCancellation(
                    sale.Status);

                // 4. Verificar pagos

                var payments = await _unitOfWork
                    .Repository<PaymentEntity>()
                    .FindAsync(p =>
                        p.SaleId == sale.Id);

                if (payments.Any())
                    throw new ArgumentException(
                        "No se puede anular una venta con pagos registrados.");

                // 5. Verificar adelantos aplicados

                var deposits = await _unitOfWork
                    .Repository<DepositEntity>()
                    .FindAsync(d =>
                        d.SaleId == sale.Id);

                if (deposits.Any())
                    throw new ArgumentException(
                        "No se puede anular una venta con adelantos aplicados.");

                // 6. Verificar comprobantes

                var invoices = await _unitOfWork
                    .Repository<InvoiceEntity>()
                    .FindAsync(i =>
                        i.SaleId == sale.Id);

                if (invoices.Any())
                    throw new ArgumentException(
                        "No se puede anular una venta con comprobantes asociados.");

                // 7. Obtener detalles

                var items = (await _unitOfWork
                    .Repository<SaleItemEntity>()
                    .FindAsync(i =>
                        i.SaleId == sale.Id))
                    .ToList();

                if (items.Any(i => i.VisitServiceId.HasValue))
                    throw new ArgumentException(
                        "Por ahora solo se pueden anular ventas sin servicios asociados.");

                // 8. Verificar movimientos de inventario

                var itemIds = items
                    .Select(i => i.Id)
                    .ToList();

                if (itemIds.Count > 0)
                {
                    var movements = await _unitOfWork
                        .Repository<MovementEntity>()
                        .FindAsync(m =>
                            m.SaleItemId.HasValue &&
                            itemIds.Contains(m.SaleItemId.Value));

                    if (movements.Any())
                        throw new ArgumentException(
                            "No se puede anular una venta con movimientos de inventario.");
                }

                // 9. Anular venta

                sale.Status = "VOIDED";

                sale.Notes =
                    $"{sale.Notes}\nANULACIÓN: {reason}"
                        .Trim();

                // 10. Guardar cambios

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return true;
    }
}
