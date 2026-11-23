using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Entities;

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
        InventoryRules.ValidateEntry(request.Quantity, 0);

        var batch = await _unitOfWork
            .Repository<Domain.Entities.ProductBatch>()
            .GetByIdAsync(request.BatchId);

        if (batch == null)
            throw new ArgumentException(
                "El lote no existe.");

        if (batch.ExpiryDate.HasValue &&
            batch.ExpiryDate.Value <
            DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException(
                "No se puede ingresar stock a un lote vencido.");

        var product = await _unitOfWork
            .Repository<Domain.Entities.Product>()
            .GetByIdAsync(batch.ProductId);

        if (product == null || !product.IsActive)
            throw new ArgumentException(
                "El producto no existe o está inactivo.");

        var movement = new InventoryMovement
        {
            BatchId = batch.Id,
            Type = "IN",
            Quantity = request.Quantity,
            UnitCost = batch.UnitCost,
            Reference = request.Reference,
            Date = DateTime.UtcNow
        };

        await _unitOfWork
            .Repository<InventoryMovement>()
            .AddAsync(movement);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}