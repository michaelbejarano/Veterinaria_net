
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services.Inventory;
using vet.Domain.Services.Sale;

namespace vet.Application.UseCases.Sale.Commands;

// ========================================
// COMMAND
// ========================================

public class ConfirmSaleCommand
    : IRequest<ConfirmSaleResponse>
{
    public long SaleId { get; set; }

    public long CashSessionId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? PaymentReference { get; set; }
}

// ========================================
// RESPONSE
// ========================================

public class ConfirmSaleResponse
{
    public long SaleId { get; set; }

    public long? PaymentId { get; set; }

    public decimal Total { get; set; }

    public decimal AppliedDeposit { get; set; }

    public decimal AmountPaid { get; set; }

    public string Status { get; set; } = string.Empty;
}

// ========================================
// HANDLER
// ========================================

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
        // ========================================
        // 1. VALIDACIONES INICIALES
        // ========================================

        if (request.SaleId <= 0)
            throw new ArgumentException(
                "La venta es obligatoria.");

        if (request.CashSessionId <= 0)
            throw new ArgumentException(
                "La sesión de caja es obligatoria.");

        var paymentMethod = request.PaymentMethod?
            .Trim()
            .ToUpperInvariant() ?? string.Empty;

        SaleRules.ValidatePaymentMethod(paymentMethod);

        var paymentReference =
            string.IsNullOrWhiteSpace(request.PaymentReference)
                ? null
                : request.PaymentReference.Trim();

        if (paymentReference is { Length: > 120 })
            throw new ArgumentException(
                "La referencia no puede superar los 120 caracteres.");

        ConfirmSaleResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // ========================================
                // 2. CONSULTAR VENTA INICIAL
                // ========================================

                var initialSales = await _unitOfWork
                    .Repository<Domain.Entities.Sale>()
                    .FindAsync(s => s.Id == request.SaleId);

                var initialSale = initialSales.SingleOrDefault();

                if (initialSale == null)
                    throw new KeyNotFoundException(
                        "La venta no existe.");

                // ========================================
                // 3. BLOQUEAR Y VALIDAR CITA
                // ========================================

                if (initialSale.AppointmentId.HasValue)
                {
                    var appointment = await _unitOfWork
                        .GetAppointmentForUpdateAsync(
                            initialSale.AppointmentId.Value,
                            ct);

                    if (appointment == null)
                        throw new KeyNotFoundException(
                            "La cita no existe.");

                    SaleRules.ValidateAppointmentStatus(
                        appointment.Status);
                }

                // ========================================
                // 4. BLOQUEAR Y VALIDAR VENTA
                // ========================================

                var sale = await _unitOfWork
                    .GetSaleForUpdateAsync(
                        request.SaleId,
                        ct);

                if (sale == null)
                    throw new KeyNotFoundException(
                        "La venta no existe.");

                SaleRules.ValidateSaleConfirmation(
                    sale.Status);

                if (sale.AppointmentId != initialSale.AppointmentId)
                    throw new ArgumentException(
                        "La cita asociada a la venta ha cambiado.");

                // Evitar confirmar una venta con pagos previos.

                var existingPayments = await _unitOfWork
                    .Repository<Payment>()
                    .FindAsync(p => p.SaleId == sale.Id);

                if (existingPayments.Any())
                    throw new ArgumentException(
                        "La venta ya tiene pagos registrados.");

                var existingApplications = await _unitOfWork
                    .Repository<DepositApplication>()
                    .FindAsync(a => a.SaleId == sale.Id);

                if (existingApplications.Any())
                    throw new ArgumentException(
                        "La venta ya tiene adelantos aplicados.");

                // ========================================
                // 5. BLOQUEAR Y VALIDAR CAJA
                // ========================================

                var cashSession = await _unitOfWork
                    .GetCashSessionForUpdateAsync(
                        request.CashSessionId,
                        ct);

                if (cashSession == null)
                    throw new KeyNotFoundException(
                        "La sesión de caja no existe.");

                SaleRules.ValidateCashSession(
                    cashSession.Status);

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
                // 6. OBTENER DETALLES Y CALCULAR TOTAL
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
                // 7. VALIDAR SERVICIOS REALIZADOS
                // ========================================

                foreach (var item in saleItems
                    .Where(i => i.VisitServiceId.HasValue))
                {
                    var visitService = await _unitOfWork
                        .Repository<Domain.Entities.VisitService>()
                        .GetByIdAsync(item.VisitServiceId!.Value);

                    if (visitService == null ||
                        visitService.Status != "COMPLETED")
                    {
                        throw new ArgumentException(
                            "La venta contiene un servicio no completado.");
                    }

                    var visit = await _unitOfWork
                        .Repository<Visit>()
                        .GetByIdAsync(visitService.VisitId);

                    if (visit == null ||
                        visit.AppointmentId != sale.AppointmentId)
                    {
                        throw new ArgumentException(
                            "El servicio no corresponde a la atención de la venta.");
                    }

                    var pet = await _unitOfWork
                        .Repository<Domain.Entities.Pet>()
                        .GetByIdAsync(visit.PetId);

                    if (pet == null || pet.OwnerId != sale.OwnerId)
                        throw new ArgumentException(
                            "El servicio no pertenece al propietario.");
                }

                // ========================================
                // 8. VALIDAR PRODUCTOS
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

                // ========================================
                // 9. BLOQUEAR LOTES DE INVENTARIO
                // ========================================

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

                    // Mantener un orden consistente de bloqueo.

                    foreach (var batch in batches.OrderBy(b => b.Id))
                    {
                        var lockedBatch = await _unitOfWork
                            .GetProductBatchForUpdateAsync(
                                batch.Id,
                                ct);

                        if (lockedBatch == null)
                            throw new ArgumentException(
                                "El lote ya no existe.");

                        // Excluir lotes vencidos.

                        if (!lockedBatch.ExpiryDate.HasValue ||
                            lockedBatch.ExpiryDate.Value >= today)
                        {
                            lockedBatches.Add(lockedBatch);
                        }
                    }
                }

                // ========================================
                // 10. CALCULAR STOCK DISPONIBLE
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
                // 11. REGISTRAR SALIDAS DE INVENTARIO
                // ========================================

                foreach (var item in saleItems
                    .Where(i => i.ProductId.HasValue)
                    .OrderBy(i => i.Id))
                {
                    // FEFO: primero los lotes próximos a vencer.

                    var productBatches = lockedBatches
                        .Where(b => b.ProductId == item.ProductId)
                        .OrderBy(b =>
                            b.ExpiryDate ?? DateOnly.MaxValue)
                        .ThenBy(b => b.Id)
                        .ToList();

                    decimal availableStock = productBatches
                        .Sum(b =>
                            Math.Max(0, availableStocks[b.Id]));

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

                    if (remainingQuantity > 0)
                        throw new ArgumentException(
                            "No existe stock suficiente para completar la venta.");
                }

                // ========================================
                // 12. APLICAR ADELANTOS DISPONIBLES
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
                        // No utilizar adelantos retenidos.

                        var forfeitures = await _unitOfWork
                            .Repository<DepositForfeiture>()
                            .FindAsync(f =>
                                f.PaymentId == deposit.Id);

                        if (forfeitures.Any())
                            continue;

                        // Consultar cuánto se utilizó anteriormente.

                        var applications = await _unitOfWork
                            .Repository<DepositApplication>()
                            .FindAsync(a =>
                                a.PaymentId == deposit.Id);

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
                // 13. CALCULAR SALDO POR COBRAR
                // ========================================

                decimal amountPaid =
                    SaleRules.CalculateRemainingBalance(
                        total,
                        appliedDeposit);

                Payment? payment = null;

                // ========================================
                // 14. REGISTRAR NUEVO PAGO
                // ========================================

                if (amountPaid > 0)
                {
                    var paymentDate = DateTime.UtcNow;

                    payment = new Payment
                    {
                        SaleId = sale.Id,

                        CashSessionId = cashSession.Id,

                        Method = paymentMethod,

                        Amount = amountPaid,

                        Reference = paymentReference,

                        Date = paymentDate,

                        Status = "CONFIRMED"
                    };

                    await _unitOfWork
                        .Repository<Payment>()
                        .AddAsync(payment);

                    // ========================================
                    // 15. REGISTRAR INGRESO EN CAJA
                    // ========================================

                    var cashMovement = new CashMovement
                    {
                        CashSessionId = cashSession.Id,

                        Payment = payment,

                        Type = "INCOME",

                        Category = "PAYMENT",

                        Method = paymentMethod,

                        Amount = amountPaid,

                        Reference = paymentReference,

                        Description = $"Cobro de venta #{sale.Id}",

                        Date = paymentDate
                    };

                    await _unitOfWork
                        .Repository<CashMovement>()
                        .AddAsync(cashMovement);
                }

                // ========================================
                // 16. CONFIRMAR VENTA
                // ========================================

                sale.Status = "CONFIRMED";

                _unitOfWork
                    .Repository<Domain.Entities.Sale>()
                    .Update(sale);

                await _unitOfWork.SaveChangesAsync(ct);

                // ========================================
                // 17. PREPARAR RESPUESTA
                // ========================================

                response = new ConfirmSaleResponse
                {
                    SaleId = sale.Id,

                    PaymentId = payment?.Id,

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
