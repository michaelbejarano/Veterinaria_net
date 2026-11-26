using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Inventory;

using ProductEntity = vet.Domain.Entities.Product;
using MovementEntity = vet.Domain.Entities.InventoryMovement;

namespace vet.Application.UseCases.Inventory.Commands;

public class RegisterInventoryEntryCommand : IRequest<long>
{
    public long BatchId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reference { get; set; }
}

public class RegisterInventoryEntryCommandHandler
    : IRequestHandler<RegisterInventoryEntryCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterInventoryEntryCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        RegisterInventoryEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BatchId <= 0)
            throw new ArgumentException(
                "El identificador del lote no es válido.");

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

                // 2. Validar cantidad y costo

                InventoryRules.ValidateEntry(
                    request.Quantity,
                    batch.UnitCost);

                // 3. Validar vencimiento

                var now = DateTime.UtcNow;

                var timeZone = TimeZoneInfo
                    .FindSystemTimeZoneById("America/Lima");

                var today = DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTimeFromUtc(
                        now, timeZone));

                if (batch.ExpiryDate.HasValue &&
                    batch.ExpiryDate.Value < today)
                    throw new ArgumentException(
                        "No se puede ingresar stock a un lote vencido.");

                // 4. Validar producto

                var product = await _unitOfWork
                    .Repository<ProductEntity>()
                    .GetByIdAsync(batch.ProductId);

                if (product is null || !product.IsActive)
                    throw new ArgumentException(
                        "El producto no existe o está inactivo.");

                // 5. Registrar movimiento

                var movement = new MovementEntity
                {
                    BatchId = batch.Id,
                    Type = "IN",
                    Quantity = request.Quantity,
                    UnitCost = batch.UnitCost,

                    Reference = string.IsNullOrWhiteSpace(
                        request.Reference)
                        ? null
                        : request.Reference.Trim(),

                    VisitServiceId = null,
                    SaleItemId = null,
                    Date = now
                };

                await _unitOfWork
                    .Repository<MovementEntity>()
                    .AddAsync(movement);

                await _unitOfWork.SaveChangesAsync(ct);

                movementId = movement.Id;
            },
            cancellationToken);

        return movementId;
    }
}