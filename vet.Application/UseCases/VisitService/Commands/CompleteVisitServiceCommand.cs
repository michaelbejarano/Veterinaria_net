using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.Commission;
using vet.Domain.Services.Inventory;
using VisitServiceEntity = vet.Domain.Entities.VisitService;

namespace vet.Application.UseCases.VisitService.Commands;

public class CompleteVisitServiceCommand
    : IRequest<CompleteVisitServiceResponse>
{
    public long AppointmentId { get; set; }

    public long VisitServiceId { get; set; }

    public List<ConsumedBatchDto> ConsumedBatches { get; set; }
        = new();
}

public class ConsumedBatchDto
{
    public long BatchId { get; set; }

    public decimal Quantity { get; set; }
}

public class CompleteVisitServiceResponse
{
    public long VisitServiceId { get; set; }

    public long VisitId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal FinalPrice { get; set; }

    public decimal CommissionAmount { get; set; }

    public int InventoryMovements { get; set; }
}

public class CompleteVisitServiceCommandHandler
    : IRequestHandler<
        CompleteVisitServiceCommand,
        CompleteVisitServiceResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompleteVisitServiceCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CompleteVisitServiceResponse> Handle(
        CompleteVisitServiceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        if (request.VisitServiceId <= 0)
            throw new ArgumentException(
                "El servicio de atención es obligatorio.");

        if (request.ConsumedBatches == null ||
            request.ConsumedBatches.Any(b =>
                b.BatchId <= 0 || b.Quantity <= 0))
        {
            throw new ArgumentException(
                "Los lotes y cantidades consumidas son inválidos.");
        }

        CompleteVisitServiceResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var now = DateTime.UtcNow;

                var today = DateOnly.FromDateTime(
                    now.AddHours(-5));

                // ========================================
                // 1. BLOQUEAR CITA
                // ========================================

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId,
                        ct);

                if (appointment == null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                if (appointment.Status != "IN_PROGRESS")
                    throw new ArgumentException(
                        "La cita debe estar en curso.");

                // ========================================
                // 2. OBTENER ATENCIÓN Y SERVICIO
                // ========================================

                var visits = await _unitOfWork
                    .Repository<Visit>()
                    .FindAsync(v =>
                        v.AppointmentId == appointment.Id);

                var visit = visits.SingleOrDefault();

                if (visit == null)
                    throw new ArgumentException(
                        "La cita no tiene una atención registrada.");

                var visitService = await _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .GetByIdAsync(request.VisitServiceId);

                if (visitService == null ||
                    visitService.VisitId != visit.Id)
                {
                    throw new ArgumentException(
                        "El servicio no pertenece a esta atención.");
                }

                VisitServiceRules.ValidateCompletion(
                    visit.Status,
                    visitService.Status);

                VisitServiceRules.ValidateFinalPrice(
                    visitService.FinalPrice);

                // ========================================
                // 3. VALIDAR ASIGNACIÓN DEL TRABAJADOR
                // ========================================

                var employeeServices = await _unitOfWork
                    .Repository<Domain.Entities.EmployeeService>()
                    .FindAsync(es =>
                        es.EmployeeId == visitService.EmployeeId &&
                        es.ServiceId == visitService.ServiceId &&
                        es.IsEnabled);

                var employeeService =
                    employeeServices.SingleOrDefault();

                if (employeeService == null)
                    throw new ArgumentException(
                        "El trabajador no está habilitado para este servicio.");

                // ========================================
                // 4. OBTENER INSUMOS DEL SERVICIO
                // ========================================

                var supplies = (await _unitOfWork
                    .Repository<ServiceSupply>()
                    .FindAsync(s =>
                        s.ServiceId == visitService.ServiceId))
                    .ToList();

                var requiredSupplies = supplies
                    .GroupBy(s => s.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(s => s.Quantity));

                var productIds = requiredSupplies.Keys.ToList();

                var products = new Dictionary<long, Domain.Entities.Product>();

                foreach (var productId in productIds)
                {
                    var product = await _unitOfWork
                        .Repository<Domain.Entities.Product>()
                        .GetByIdAsync(productId);

                    if (product == null || !product.IsActive)
                        throw new ArgumentException(
                            $"El insumo {productId} no está disponible.");

                    products[productId] = product;
                }

                // ========================================
                // 5. BLOQUEAR LOTES
                // ========================================

                var lockedBatches = new List<Domain.Entities.ProductBatch>();

                if (productIds.Count > 0)
                {
                    var batches = await _unitOfWork
                        .Repository<Domain.Entities.ProductBatch>()
                        .FindAsync(b =>
                            productIds.Contains(b.ProductId));

                    foreach (var batch in batches.OrderBy(b => b.Id))
                    {
                        var lockedBatch = await _unitOfWork
                            .GetProductBatchForUpdateAsync(
                                batch.Id,
                                ct);

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
                // 6. CALCULAR STOCK DISPONIBLE
                // ========================================

                var availableStocks =
                    new Dictionary<long, decimal>();

                foreach (var batch in lockedBatches)
                {
                    var movements = await _unitOfWork
                        .Repository<InventoryMovement>()
                        .FindAsync(m =>
                            m.BatchId == batch.Id);

                    availableStocks[batch.Id] =
                        InventoryRules.CalculateStock(
                            movements.Select(m =>
                                (m.Type, m.Quantity)));
                }

                // ========================================
                // 7. VALIDAR LOTES INDICADOS
                // ========================================

                var selectedBatches = request.ConsumedBatches
                    .GroupBy(b => b.BatchId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(b => b.Quantity));

                foreach (var selected in selectedBatches)
                {
                    if (!lockedBatches.Any(b =>
                        b.Id == selected.Key))
                    {
                        throw new ArgumentException(
                            "El lote indicado está vencido o no corresponde al servicio.");
                    }
                }

                int movementCount = 0;

                // ========================================
                // 8. REGISTRAR CONSUMO DE INSUMOS
                // ========================================

                foreach (var supply in requiredSupplies)
                {
                    long productId = supply.Key;
                    decimal requiredQuantity = supply.Value;

                    var product = products[productId];

                    var productBatches = lockedBatches
                        .Where(b => b.ProductId == productId)
                        .OrderBy(b =>
                            b.ExpiryDate ?? DateOnly.MaxValue)
                        .ThenBy(b => b.Id)
                        .ToList();

                    var explicitBatches = productBatches
                        .Where(b =>
                            selectedBatches.ContainsKey(b.Id))
                        .ToList();

                    if (explicitBatches.Count > 0)
                    {
                        decimal selectedQuantity =
                            explicitBatches.Sum(b =>
                                selectedBatches[b.Id]);

                        if (selectedQuantity != requiredQuantity)
                            throw new ArgumentException(
                                $"La cantidad seleccionada de {product.Name} debe ser {requiredQuantity}.");
                    }
                    else if (product.TrackBatches ||
                             product.Type is "VACCINE" or "MEDICINE")
                    {
                        throw new ArgumentException(
                            $"Debes indicar el lote utilizado de {product.Name}.");
                    }

                    var batchesToUse = explicitBatches.Count > 0
                        ? explicitBatches
                        : productBatches;

                    decimal availableStock = batchesToUse
                        .Sum(b =>
                            Math.Max(0, availableStocks[b.Id]));

                    InventoryRules.ValidateAvailableStock(
                        requiredQuantity,
                        availableStock);

                    decimal remainingQuantity = requiredQuantity;

                    foreach (var batch in batchesToUse)
                    {
                        decimal batchStock =
                            availableStocks[batch.Id];

                        decimal quantityToUse =
                            explicitBatches.Count > 0
                                ? selectedBatches[batch.Id]
                                : Math.Min(
                                    remainingQuantity,
                                    Math.Max(0, batchStock));

                        if (quantityToUse <= 0)
                            continue;

                        InventoryRules.ValidateAvailableStock(
                            quantityToUse,
                            batchStock);

                        var movement = new InventoryMovement
                        {
                            BatchId = batch.Id,
                            VisitServiceId = visitService.Id,
                            SaleItemId = null,
                            Type = "OUT",
                            Quantity = quantityToUse,
                            UnitCost = batch.UnitCost,
                            Reference = $"ATENCION-{visit.Id}",
                            Date = now
                        };

                        await _unitOfWork
                            .Repository<InventoryMovement>()
                            .AddAsync(movement);

                        availableStocks[batch.Id] -= quantityToUse;

                        remainingQuantity -= quantityToUse;

                        movementCount++;

                        if (remainingQuantity == 0)
                            break;
                    }

                    if (remainingQuantity != 0)
                        throw new ArgumentException(
                            "No se pudo completar el consumo de insumos.");
                }

                // ========================================
                // 9. VALIDAR COMISIÓN EXISTENTE
                // ========================================

                var existingCommissions = await _unitOfWork
                    .Repository<Commission>()
                    .FindAsync(c =>
                        c.VisitServiceId == visitService.Id);

                if (existingCommissions.Any())
                    throw new ArgumentException(
                        "El servicio ya tiene una comisión registrada.");

                // ========================================
                // 10. OBTENER REGLA DE COMISIÓN VIGENTE
                // ========================================

                var rules = await _unitOfWork
                    .Repository<CommissionRule>()
                    .FindAsync(r =>
                        r.EmployeeServiceId == employeeService.Id);

                var activeRules = rules
                    .Where(r =>
                        CommissionRules.IsApplicable(
                            r.ValidFrom,
                            r.ValidTo,
                            today))
                    .ToList();

                if (activeRules.Count > 1)
                    throw new ArgumentException(
                        "Existe más de una regla de comisión vigente.");

                var rule = activeRules.SingleOrDefault();

                decimal commissionAmount = 0;

                // ========================================
                // 11. GENERAR COMISIÓN
                // ========================================

                if (rule != null)
                {
                    CommissionRules.ValidateRule(
                        rule.Type,
                        rule.Value);

                    if (rule.Type != "NONE")
                    {
                        commissionAmount =
                            CommissionRules.CalculateAmount(
                                rule.Type,
                                rule.Value,
                                visitService.FinalPrice);

                        var commission = new Commission
                        {
                            VisitServiceId = visitService.Id,
                            EmployeeId = visitService.EmployeeId,
                            RuleId = rule.Id,
                            AppliedType = rule.Type,
                            AppliedValue = rule.Value,
                            CalculationBase = visitService.FinalPrice,
                            Amount = commissionAmount,
                            Status = "PENDING",
                            Date = now
                        };

                        await _unitOfWork
                            .Repository<Commission>()
                            .AddAsync(commission);
                    }
                }

                // ========================================
                // 12. COMPLETAR SERVICIO
                // ========================================

                visitService.Status = "COMPLETED";

                _unitOfWork
                    .Repository<VisitServiceEntity>()
                    .Update(visitService);

                await _unitOfWork.SaveChangesAsync(ct);

                // ========================================
                // 13. PREPARAR RESPUESTA
                // ========================================

                response = new CompleteVisitServiceResponse
                {
                    VisitServiceId = visitService.Id,
                    VisitId = visit.Id,
                    Status = visitService.Status,
                    FinalPrice = visitService.FinalPrice,
                    CommissionAmount = commissionAmount,
                    InventoryMovements = movementCount
                };
            },
            cancellationToken);

        return response!;
    }
}