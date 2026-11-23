using MediatR;
using vet.Domain.Ports;
using ProductEntity = vet.Domain.Entities.Product;

namespace vet.Application.UseCases.Product.Queries;

public record GetAllProductsQuery : IRequest<List<ProductResponse>>;

public class ProductResponse
{
    public long Id { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public bool TrackBatches { get; set; }
    public bool IsActive { get; set; }
}

public class GetAllProductsQueryHandler
    : IRequestHandler<GetAllProductsQuery, List<ProductResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllProductsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductResponse>> Handle(
        GetAllProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await _unitOfWork
            .Repository<ProductEntity>()
            .GetAllAsync();

        return products
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Type = p.Type,
                Unit = p.Unit,
                SalePrice = p.SalePrice,
                TrackBatches = p.TrackBatches,
                IsActive = p.IsActive
            })
            .ToList();
    }
}