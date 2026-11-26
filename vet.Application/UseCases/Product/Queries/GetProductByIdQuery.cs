using MediatR;
using vet.Domain.Ports;

using ProductEntity = vet.Domain.Entities.Product;

namespace vet.Application.UseCases.Product.Queries;

public record GetProductByIdQuery(long Id)
    : IRequest<ProductResponse>;

public class GetProductByIdQueryHandler
    : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.Id);

        if (product is null)
            throw new KeyNotFoundException(
                "El producto no existe.");

        return new ProductResponse
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Type = product.Type,
            Unit = product.Unit,
            SalePrice = product.SalePrice,
            TrackBatches = product.TrackBatches,
            IsActive = product.IsActive
        };
    }
}