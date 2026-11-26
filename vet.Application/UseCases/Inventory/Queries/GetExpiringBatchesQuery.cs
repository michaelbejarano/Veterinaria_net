using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Inventory;

using ProductEntity = vet.Domain.Entities.Product;
using BatchEntity = vet.Domain.Entities.ProductBatch;
using MovementEntity = vet.Domain.Entities.InventoryMovement;

namespace vet.Application.UseCases.Inventory.Queries;

public class GetExpiringBatchesQuery
    : IRequest<List<ExpiringBatchDto>>
{
    public int Days { get; set; } = 30;
    public bool IncludeExpired { get; set; } = true;
}

public class ExpiringBatchDto
{
    public long BatchId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public decimal Stock { get; set; }
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class GetExpiringBatchesQueryHandler
    : IRequestHandler<
        GetExpiringBatchesQuery,
        List<ExpiringBatchDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetExpiringBatchesQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ExpiringBatchDto>> Handle(
        GetExpiringBatchesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Days < 0 || request.Days > 365)
            throw new ArgumentException(
                "Los días deben estar entre 0 y 365.");

        var timeZone = TimeZoneInfo
            .FindSystemTimeZoneById("America/Lima");

        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, timeZone));

        var limitDate = today.AddDays(request.Days);

        // 1. Buscar lotes próximos a vencer

        var batches = await _unitOfWork
            .Repository<BatchEntity>()
            .FindAsync(b =>
                b.ExpiryDate.HasValue &&
                b.ExpiryDate.Value <= limitDate &&
                (request.IncludeExpired ||
                 b.ExpiryDate.Value >= today));

        if (!batches.Any())
            return new List<ExpiringBatchDto>();

        var batchIds = batches
            .Select(b => b.Id)
            .ToList();

        var productIds = batches
            .Select(b => b.ProductId)
            .Distinct()
            .ToList();

        // 2. Buscar productos

        var products = await _unitOfWork
            .Repository<ProductEntity>()
            .FindAsync(p => productIds.Contains(p.Id));

        var productDictionary = products
            .ToDictionary(p => p.Id);

        // 3. Buscar movimientos

        var movements = await _unitOfWork
            .Repository<MovementEntity>()
            .FindAsync(m => batchIds.Contains(m.BatchId));

        var movementsByBatch = movements
            .ToLookup(m => m.BatchId);

        // 4. Calcular stock y vencimiento

        var result = new List<ExpiringBatchDto>();

        foreach (var batch in batches)
        {
            var stock = InventoryRules.CalculateStock(
                movementsByBatch[batch.Id]
                    .Select(m => (m.Type, m.Quantity)));

            if (stock <= 0)
                continue;

            var expiryDate = batch.ExpiryDate!.Value;

            result.Add(new ExpiringBatchDto
            {
                BatchId = batch.Id,
                ProductId = batch.ProductId,
                ProductName = productDictionary[batch.ProductId].Name,
                BatchCode = batch.BatchCode,
                ExpiryDate = expiryDate,
                Stock = stock,

                DaysRemaining =
                    expiryDate.DayNumber - today.DayNumber,

                Status = expiryDate < today
                    ? "EXPIRED"
                    : "EXPIRING"
            });
        }

        return result
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.BatchId)
            .ToList();
    }
}