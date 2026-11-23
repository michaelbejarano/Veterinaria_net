using MediatR;
using vet.Domain.Ports;
using ProductBatchEntity = vet.Domain.Entities.ProductBatch;

namespace vet.Application.UseCases.ProductBatch.Queries;

public record GetProductBatchesQuery(long ProductId)
    : IRequest<List<ProductBatchResponse>>;

public class ProductBatchResponse
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
}

public class GetProductBatchesQueryHandler
    : IRequestHandler<GetProductBatchesQuery, List<ProductBatchResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductBatchesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductBatchResponse>> Handle(
        GetProductBatchesQuery request,
        CancellationToken cancellationToken)
    {
        var batches = await _unitOfWork
            .Repository<ProductBatchEntity>()
            .FindAsync(b => b.ProductId == request.ProductId);

        return batches
            .Select(b => new ProductBatchResponse
            {
                Id = b.Id,
                ProductId = b.ProductId,
                BatchCode = b.BatchCode,
                ExpiryDate = b.ExpiryDate,
                UnitCost = b.UnitCost
            })
            .OrderBy(b => b.ExpiryDate)
            .ToList();
    }
}