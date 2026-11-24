using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using ProductEntity = vet.Domain.Entities.Product;
using ProductBatchEntity = vet.Domain.Entities.ProductBatch;

namespace vet.Application.UseCases.ProductBatch.Commands;

public class CreateProductBatchCommand : IRequest<long>
{
    public long ProductId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
}

public class CreateProductBatchCommandHandler
    : IRequestHandler<CreateProductBatchCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductBatchCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateProductBatchCommand request,
        CancellationToken cancellationToken)
    {
        ProductBatchRules.Validate(
            request.BatchCode,
            request.ExpiryDate,
            request.UnitCost);

        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.ProductId);

        if (product == null)
            throw new ArgumentException(
                "El producto no existe.");

        if (!product.IsActive)
            throw new ArgumentException(
                "El producto está inactivo.");

        var batch = new ProductBatchEntity
        {
            ProductId = request.ProductId,
            BatchCode = request.BatchCode.Trim(),
            ExpiryDate = request.ExpiryDate,
            UnitCost = request.UnitCost
        };

        await _unitOfWork
            .Repository<ProductBatchEntity>()
            .AddAsync(batch);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return batch.Id;
    }
}