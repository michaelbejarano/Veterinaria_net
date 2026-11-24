using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;

namespace vet.Application.UseCases.Sale.Commands;

public class ConfirmSaleCommand : IRequest<ConfirmSaleResponse>
{
    public long SaleId { get; set; }

    public long CashSessionId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? PaymentReference { get; set; }
}

public class ConfirmSaleResponse
{
    public long SaleId { get; set; }

    public long? PaymentId { get; set; }

    public decimal Total { get; set; }

    public decimal AppliedDeposit { get; set; }

    public decimal AmountPaid { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class ConfirmSaleCommandHandler
    : IRequestHandler<ConfirmSaleCommand, ConfirmSaleResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmSaleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfirmSaleResponse> Handle(
        ConfirmSaleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SaleId <= 0)
            throw new ArgumentException("La venta es obligatoria.");

        if (request.CashSessionId <= 0)
            throw new ArgumentException("La sesión de caja es obligatoria.");

        SaleRules.ValidatePaymentMethod(request.PaymentMethod);

        ConfirmSaleResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // ========================================
                // 1. CONSULTAR VENTA
                // ========================================
                var initialSales = await _unitOfWork
                    .Repository<Domain.Entities.Sale>()
                    .FindAsync(s => s.Id == request.SaleId);

                var initialSale = initialSales.SingleOrDefault();

                if (initialSale == null)
                    throw new ArgumentException("La venta no existe.");

                // ========================================
                // 2. BLOQUEAR CITA ASOCIADA
                // ========================================
                if (initialSale.AppointmentId.HasValue)
                {
                    var appointment = await _unitOfWork
                        .GetAppointmentForUpdateAsync(
                            initialSale.AppointmentId.Value,
                            ct);

                    if (appointment == null)
                        throw new ArgumentException("La cita no existe.");
                }

                // ========================================
                // 3. BLOQUEAR Y VALIDAR VENTA
                // ========================================
                var sale = await _unitOfWork
                    .GetSaleForUpdateAsync(request.SaleId, ct);

                if (sale == null)
                    throw new ArgumentException("La venta no existe.");

                SaleRules.ValidateSaleConfirmation(sale.Status);

                if (sale.AppointmentId != initialSale.AppointmentId)
                    throw new ArgumentException(
                        "La cita asociada a la venta ha cambiado.");

                // ========================================
                // 4. VALIDAR SESIÓN DE CAJA
                // ========================================
                var cashSession = await _unitOfWork
                    .GetCashSessionForUpdateAsync(
                        request.CashSessionId,
                        ct);

                if (cashSession == null)
                    throw new ArgumentException(
                        "La sesión de caja no existe.");

                SaleRules.ValidateCashSession(cashSession.Status);

                var cashRegister = await _unitOfWork
                    .Repository<Domain.Entities.CashRegister>()
                    .GetByIdAsync(cashSession.CashRegisterId);

                if (cashRegister == null || !cashRegister.IsActive)
                    throw new ArgumentException(
                        "La caja no existe o está inactiva.");

                SaleRules.ValidateCompany(
                    sale.CompanyId,
                    cashRegister.CompanyId);

                // ========================================
                // 5. OBTENER DETALLES Y CALCULAR TOTAL
                // ========================================
                var saleItems = (await _unitOfWork
                    .Repository<SaleItem>()
                    .FindAsync(i => i.SaleId == sale.Id))
                    .ToList();

                SaleRules.ValidateSale(
                    sale.OwnerId,
                    saleItems.Count);

                decimal total = saleItems.Sum(item =>
                    SaleRules.CalculateItemTotal(
                        item.Quantity,
                        item.UnitPrice,
                        item.Discount,
                        item.Igv));

                // ========================================
                // 6. VALIDAR SERVICIOS
                // ========================================
                foreach (var item in saleItems
                    .Where(i => i.VisitServiceId.HasValue))
                {
                    var visitService = await _unitOfWork
                        .Repository<VisitService>()
                        .GetByIdAsync(item.VisitServiceId!.Value);

                    if (visitService == null ||
                        visitService.Status != "COMPLETED")
                    {
                        throw new ArgumentException(
                            "La venta contiene un servicio no completado.");
                    }
                }

                // ========================================
                // 7. OBTENER PRODUCTOS Y LOTES
                // ========================================
                var productIds = saleItems
                    .Where(i => i.ProductId.HasValue)
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();

                foreach (var productId in productIds)
                {
                    var product = await _unitOfWork
                        .Repository<Domain.Entities.Product>()
                        .GetByIdAsync(productId);

                    if (product == null || !product.IsActive)
                        throw new ArgumentException(
                            $"El producto {productId} no está disponible.");
                }

                var today = DateOnly.FromDateTime(
                    DateTime.UtcNow.AddHours(-5));

                var lockedBatches =
                    new List<Domain.Entities.ProductBatch>();

                if (productIds.Count > 0)
                {
                    var batches = await _unitOfWork
                        .Repository<Domain.Entities.ProductBatch>()
                        .FindAsync(b =>
                            productIds.Contains(b.ProductId));

                    // Bloquear siempre en orden ascendente por ID.
                    foreach (var batch in batches.OrderBy(b => b.Id))
                    {
                        var lockedBatch = await _unitOfWork
                            .GetProductBatchForUpdateAsync(batch.Id, ct);

                        if (lockedBatch == null)
                            throw new ArgumentException(
                                "El lote ya no existe.");

                        if (!lockedBatch.ExpiryDate.HasValue ||
                            lockedBatch.ExpiryDate.Value >= today)
                        {
                            lockedBatches.Add(lockedBatch);
                        }
                    }
                }

                // ========================================
                // 8. CALCULAR STOCK DISPONIBLE
                // ========================================
                var availableStocks =
                    new Dictionary<long, decimal>();

                foreach (var batch in lockedBatches)
                {
                    var movements = await _unitOfWork
                        .Repository<InventoryMovement>()
                        .FindAsync(m => m.BatchId == batch.Id);

                    availableStocks[batch.Id] =
                        InventoryRules.CalculateStock(
                            movements.Select(m =>
                                (m.Type, m.Quantity)));
                }

                // ========================================
                // 9. REGISTRAR SALIDAS DE INVENTARIO
                // ========================================
                foreach (var item in saleItems
                    .Where(i => i.ProductId.HasValue)
                    .OrderBy(i => i.Id))
                {
                    var productBatches = lockedBatches
                        .Where(b => b.ProductId == item.ProductId)
                        .OrderBy(b =>
                            b.ExpiryDate ?? DateOnly.MaxValue)
                        .ThenBy(b => b.Id)
                        .ToList();

                    decimal availableStock = productBatches
                        .Sum(b => Math.Max(0, availableStocks[b.Id]));

                    SaleRules.ValidateProductQuantity(
                        item.Quantity,
                        availableStock);

                    decimal remainingQuantity = item.Quantity;

                    foreach (var batch in productBatches)
                    {
                        decimal batchStock =
                            availableStocks[batch.Id];

                        if (batchStock <= 0)
                            continue;

                        decimal quantityToUse = Math.Min(
                            remainingQuantity,
                            batchStock);

                        var movement = new InventoryMovement
                        {
                            BatchId = batch.Id,
                            SaleItemId = item.Id,
                            Type = "OUT",
                            Quantity = quantityToUse,
                            UnitCost = batch.UnitCost,
                            Reference = $"VENTA-{sale.Id}",
                            Date = DateTime.UtcNow
                        };

                        await _unitOfWork
                            .Repository<InventoryMovement>()
                            .AddAsync(movement);

                        availableStocks[batch.Id] -= quantityToUse;

                        remainingQuantity -= quantityToUse;

                        if (remainingQuantity == 0)
                            break;
                    }
                }

                // ========================================
                // 10. APLICAR ADELANTOS DE LA CITA
                // ========================================
                decimal appliedDeposit = 0;

                if (sale.AppointmentId.HasValue &&
                    saleItems.Any(i => i.VisitServiceId.HasValue))
                {
                    var deposits = await _unitOfWork
                        .Repository<Payment>()
                        .FindAsync(p =>
                            p.AppointmentId == sale.AppointmentId &&
                            p.Status == "CONFIRMED");

                    foreach (var deposit in deposits.OrderBy(p => p.Id))
                    {
                        var forfeitures = await _unitOfWork
                            .Repository<DepositForfeiture>()
                            .FindAsync(f => f.PaymentId == deposit.Id);

                        // No utilizar adelantos retenidos.
                        if (forfeitures.Any())
                            continue;

                        var applications = await _unitOfWork
                            .Repository<DepositApplication>()
                            .FindAsync(a => a.PaymentId == deposit.Id);

                        decimal alreadyApplied =
                            applications.Sum(a => a.Amount);

                        decimal availableDeposit =
                            deposit.Amount - alreadyApplied;

                        if (availableDeposit <= 0)
                            continue;

                        decimal amountToApply = Math.Min(
                            availableDeposit,
                            total - appliedDeposit);

                        if (amountToApply <= 0)
                            break;

                        var application = new DepositApplication
                        {
                            PaymentId = deposit.Id,
                            SaleId = sale.Id,
                            Amount = amountToApply
                        };

                        await _unitOfWork
                            .Repository<DepositApplication>()
                            .AddAsync(application);

                        appliedDeposit += amountToApply;

                        if (appliedDeposit == total)
                            break;
                    }
                }

                // ========================================
                // 11. CALCULAR SALDO PENDIENTE
                // ========================================
                decimal amountPaid =
                    SaleRules.CalculateRemainingBalance(
                        total,
                        appliedDeposit);

                long? paymentId = null;

                // ========================================
                // 12. REGISTRAR PAGO
                // ========================================
                if (amountPaid > 0)
                {
                    var payment = new Payment
                    {
                        SaleId = sale.Id,
                        CashSessionId = cashSession.Id,
                        Method = request.PaymentMethod,
                        Amount = amountPaid,
                        Reference = request.PaymentReference,
                        Date = DateTime.UtcNow,
                        Status = "CONFIRMED"
                    };

                    await _unitOfWork
                        .Repository<Payment>()
                        .AddAsync(payment);

                    await _unitOfWork.SaveChangesAsync(ct);

                    paymentId = payment.Id;

                    // ========================================
                    // 13. REGISTRAR INGRESO EN CAJA
                    // ========================================
                    var cashMovement = new CashMovement
                    {
                        CashSessionId = cashSession.Id,
                        PaymentId = payment.Id,
                        Type = "INCOME",
                        Category = "PAYMENT",
                        Method = request.PaymentMethod,
                        Amount = amountPaid,
                        Reference = request.PaymentReference,
                        Description = $"Cobro de venta #{sale.Id}",
                        Date = DateTime.UtcNow
                    };

                    await _unitOfWork
                        .Repository<CashMovement>()
                        .AddAsync(cashMovement);
                }

                // ========================================
                // 14. CONFIRMAR VENTA
                // ========================================
                sale.Status = "CONFIRMED";

                _unitOfWork
                    .Repository<Domain.Entities.Sale>()
                    .Update(sale);

                await _unitOfWork.SaveChangesAsync(ct);

                // ========================================
                // 15. PREPARAR RESPUESTA
                // ========================================
                response = new ConfirmSaleResponse
                {
                    SaleId = sale.Id,
                    PaymentId = paymentId,
                    Total = total,
                    AppliedDeposit = appliedDeposit,
                    AmountPaid = amountPaid,
                    Status = sale.Status
                };
            },
            cancellationToken);

        return response!;
    }
}
