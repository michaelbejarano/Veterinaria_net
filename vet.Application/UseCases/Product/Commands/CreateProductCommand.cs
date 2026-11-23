using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using ProductEntity = vet.Domain.Entities.Product;

namespace vet.Application.UseCases.Product.Commands;

public class CreateProductCommand : IRequest<long>
{
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public bool TrackBatches { get; set; }
}

public class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        ProductRules.Validate(
            request.Name,
            request.Type,
            request.Unit,
            request.SalePrice);

        var product = new ProductEntity
        {
            Sku = request.Sku,
            Name = request.Name.Trim(),
            Type = request.Type,
            Unit = request.Unit.Trim(),
            SalePrice = request.SalePrice,
            TrackBatches = request.TrackBatches,
            IsActive = true
        };

        await _unitOfWork
            .Repository<ProductEntity>()
            .AddAsync(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}