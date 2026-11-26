using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Inventory;

using ProductEntity = vet.Domain.Entities.Product;
using BatchEntity = vet.Domain.Entities.ProductBatch;
using MovementEntity = vet.Domain.Entities.InventoryMovement;

namespace vet.Application.UseCases.Inventory.Queries;

public record GetProductStockQuery(long ProductId)
    : IRequest<ProductStockResponse>;

public class ProductStockResponse
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal TotalStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal ExpiredStock { get; set; }

    public List<ProductStockBatchDto> Batches { get; set; }
        = new();
}

public class ProductStockBatchDto
{
    public long BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Stock { get; set; }
    public bool IsExpired { get; set; }
}

public class GetProductStockQueryHandler
    : IRequestHandler<GetProductStockQuery, ProductStockResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductStockQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductStockResponse> Handle(
        GetProductStockQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar producto

        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.ProductId);

        if (product is null)
            throw new KeyNotFoundException(
                "El producto no existe.");

        // 2. Buscar lotes

        var batches = await _unitOfWork
            .Repository<BatchEntity>()
            .FindAsync(b => b.ProductId == product.Id);

        var batchIds = batches
            .Select(b => b.Id)
            .ToList();

        // 3. Buscar movimientos

        var movements = await _unitOfWork
            .Repository<MovementEntity>()
            .FindAsync(m => batchIds.Contains(m.BatchId));

        var movementsByBatch = movements
            .ToLookup(m => m.BatchId);

        var timeZone = TimeZoneInfo
            .FindSystemTimeZoneById("America/Lima");

        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, timeZone));

        // 4. Calcular stock por lote

        var batchResults = batches
            .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
            .ThenBy(b => b.Id)
            .Select(b => new ProductStockBatchDto
            {
                BatchId = b.Id,
                BatchCode = b.BatchCode,
                ExpiryDate = b.ExpiryDate,
                UnitCost = b.UnitCost,

                Stock = InventoryRules.CalculateStock(
                    movementsByBatch[b.Id]
                        .Select(m => (m.Type, m.Quantity))),

                IsExpired = b.ExpiryDate.HasValue &&
                            b.ExpiryDate.Value < today
            })
            .ToList();

        // 5. Construir respuesta

        return new ProductStockResponse
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Unit = product.Unit,
            IsActive = product.IsActive,

            TotalStock = batchResults.Sum(b => b.Stock),

            AvailableStock = product.IsActive
                ? batchResults
                    .Where(b => !b.IsExpired)
                    .Sum(b => Math.Max(0m, b.Stock))
                : 0m,

            ExpiredStock = batchResults
                .Where(b => b.IsExpired)
                .Sum(b => b.Stock),

            Batches = batchResults
        };
    }
}