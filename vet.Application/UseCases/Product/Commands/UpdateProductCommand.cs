using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using ProductEntity = vet.Domain.Entities.Product;
using ProductBatchEntity = vet.Domain.Entities.ProductBatch;

namespace vet.Application.UseCases.Product.Commands;

public class UpdateProductCommand : IRequest<bool>
{
    public long Id { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public bool TrackBatches { get; set; }
}

public class UpdateProductCommandHandler
    : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
            throw new ArgumentException(
                "El identificador del producto no es válido.");

        var type = request.Type.Trim().ToUpperInvariant();

        var sku = string.IsNullOrWhiteSpace(request.Sku)
            ? null
            : request.Sku.Trim();

        ProductRules.Validate(
            request.Name,
            type,
            request.Unit,
            request.SalePrice);

        var repository = _unitOfWork
            .Repository<ProductEntity>();

        // 1. Buscar producto

        var product = await repository
            .GetByIdAsync(request.Id);

        if (product is null)
            throw new KeyNotFoundException(
                "El producto no existe.");

        // 2. Validar SKU duplicado

        if (sku is not null)
        {
            var existing = await repository.FindAsync(p =>
                p.Sku == sku &&
                p.Id != request.Id);

            if (existing.Any())
                throw new ArgumentException(
                    "El SKU pertenece a otro producto.");
        }

        // 3. Proteger el historial de inventario

        var batches = await _unitOfWork
            .Repository<ProductBatchEntity>()
            .FindAsync(b => b.ProductId == product.Id);

        if (batches.Any())
        {
            if (product.Type != type)
                throw new ArgumentException(
                    "No se puede cambiar el tipo de un producto con lotes.");

            if (product.Unit != request.Unit.Trim())
                throw new ArgumentException(
                    "No se puede cambiar la unidad de un producto con lotes.");

            if (product.TrackBatches != request.TrackBatches)
                throw new ArgumentException(
                    "No se puede modificar el control de lotes.");
        }

        // 4. Actualizar producto

        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Type = type;
        product.Unit = request.Unit.Trim();
        product.SalePrice = request.SalePrice;
        product.TrackBatches = request.TrackBatches;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}