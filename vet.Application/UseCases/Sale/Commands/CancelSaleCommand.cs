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
        if (request.SaleId <= 0)
            throw new ArgumentException(
                "La venta es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException(
                "Debe indicar el motivo de anulación.");

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear venta

                var sale = await _unitOfWork
                    .GetSaleForUpdateAsync(
                        request.SaleId, ct);

                if (sale is null)
                    throw new KeyNotFoundException(
                        "La venta no existe.");

                SaleRules.ValidateSaleCancellation(
                    sale.Status);

                // 2. Verificar pagos

                var payments = await _unitOfWork
                    .Repository<PaymentEntity>()
                    .FindAsync(x => x.SaleId == sale.Id);

                if (payments.Any())
                    throw new ArgumentException(
                        "La venta tiene pagos registrados.");

                // 3. Verificar adelantos aplicados

                var deposits = await _unitOfWork
                    .Repository<DepositEntity>()
                    .FindAsync(x => x.SaleId == sale.Id);

                if (deposits.Any())
                    throw new ArgumentException(
                        "La venta tiene adelantos aplicados.");

                // 4. Verificar comprobantes

                var invoices = await _unitOfWork
                    .Repository<InvoiceEntity>()
                    .FindAsync(x => x.SaleId == sale.Id);

                if (invoices.Any())
                    throw new ArgumentException(
                        "La venta tiene comprobantes asociados.");

                // 5. Verificar movimientos de inventario

                var items = await _unitOfWork
                    .Repository<SaleItemEntity>()
                    .FindAsync(x => x.SaleId == sale.Id);

                if (items.Any(x => x.VisitServiceId.HasValue))
                    throw new ArgumentException(
                        "Por ahora solo se pueden anular ventas sin servicios asociados.");

                var itemIds = items
                    .Select(x => x.Id)
                    .ToList();

                var movements = await _unitOfWork
                    .Repository<MovementEntity>()
                    .FindAsync(x =>
                        x.SaleItemId.HasValue &&
                        itemIds.Contains(x.SaleItemId.Value));

                if (movements.Any())
                    throw new ArgumentException(
                        "La venta tiene movimientos de inventario.");

                // 6. Anular venta

                sale.Status = "VOIDED";

                sale.Notes =
                    $"{sale.Notes}\nANULACIÓN: {request.Reason.Trim()}"
                        .Trim();

                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return true;
    }
}