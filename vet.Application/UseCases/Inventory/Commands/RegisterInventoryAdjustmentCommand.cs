using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Inventory;

using ProductEntity = vet.Domain.Entities.Product;
using MovementEntity = vet.Domain.Entities.InventoryMovement;

namespace vet.Application.UseCases.Inventory.Commands;

public class RegisterInventoryAdjustmentCommand : IRequest<long>
{
    public long BatchId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public class RegisterInventoryAdjustmentCommandHandler
    : IRequestHandler<RegisterInventoryAdjustmentCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterInventoryAdjustmentCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        RegisterInventoryAdjustmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BatchId <= 0)
            throw new ArgumentException(
                "El identificador del lote no es válido.");

        var type = request.Type?.Trim().ToUpperInvariant();

        if (type != "ADJUSTMENT_IN" &&
            type != "ADJUSTMENT_OUT")
            throw new ArgumentException(
                "El tipo de ajuste no es válido.");

        if (string.IsNullOrWhiteSpace(request.Reference))
            throw new ArgumentException(
                "Debe indicar el motivo del ajuste.");

        long movementId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear lote

                var batch = await _unitOfWork
                    .GetProductBatchForUpdateAsync(
                        request.BatchId, ct);

                if (batch is null)
                    throw new KeyNotFoundException(
                        "El lote no existe.");

                InventoryRules.ValidateEntry(
                    request.Quantity,
                    batch.UnitCost);

                // 2. Buscar producto

                var product = await _unitOfWork
                    .Repository<ProductEntity>()
                    .GetByIdAsync(batch.ProductId);

                if (product is null)
                    throw new KeyNotFoundException(
                        "El producto no existe.");

                var now = DateTime.UtcNow;

                var timeZone = TimeZoneInfo
                    .FindSystemTimeZoneById("America/Lima");

                var today = DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTimeFromUtc(
                        now, timeZone));

                var repository = _unitOfWork
                    .Repository<MovementEntity>();

                // 3. Validar ajuste de entrada

                if (type == "ADJUSTMENT_IN")
                {
                    if (!product.IsActive)
                        throw new ArgumentException(
                            "El producto está inactivo.");

                    if (batch.ExpiryDate.HasValue &&
                        batch.ExpiryDate.Value < today)
                        throw new ArgumentException(
                            "El lote está vencido.");
                }

                // 4. Validar ajuste de salida

                if (type == "ADJUSTMENT_OUT")
                {
                    var movements = await repository
                        .FindAsync(m => m.BatchId == batch.Id);

                    var stock = InventoryRules.CalculateStock(
                        movements.Select(m =>
                            (m.Type, m.Quantity)));

                    InventoryRules.ValidateAvailableStock(
                        request.Quantity, stock);
                }

                // 5. Registrar ajuste

                var movement = new MovementEntity
                {
                    BatchId = batch.Id,
                    Type = type,
                    Quantity = request.Quantity,
                    UnitCost = batch.UnitCost,
                    Reference = request.Reference.Trim(),
                    VisitServiceId = null,
                    SaleItemId = null,
                    Date = now
                };

                await repository.AddAsync(movement);

                await _unitOfWork.SaveChangesAsync(ct);

                movementId = movement.Id;
            },
            cancellationToken);

        return movementId;
    }
}