using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Entities;
using vet.Domain.Services.Inventory;

namespace vet.Application.UseCases.Inventory.Queries;

public record GetBatchStockQuery(long BatchId)
    : IRequest<BatchStockResponse>;

public class BatchStockResponse
{
    public long BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }
}

public class GetBatchStockQueryHandler
    : IRequestHandler<GetBatchStockQuery, BatchStockResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBatchStockQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BatchStockResponse> Handle(
        GetBatchStockQuery request,
        CancellationToken cancellationToken)
    {
        var batch = await _unitOfWork
            .Repository<Domain.Entities.ProductBatch>()
            .GetByIdAsync(request.BatchId);

        if (batch == null)
            throw new ArgumentException("El lote no existe.");

        var movements = await _unitOfWork
            .Repository<InventoryMovement>()
            .FindAsync(m => m.BatchId == request.BatchId);

        var stock = InventoryRules.CalculateStock(
            movements.Select(m => (m.Type, m.Quantity)));

        return new BatchStockResponse
        {
            BatchId = batch.Id,
            BatchCode = batch.BatchCode,
            AvailableStock = stock
        };
    }
}