using MediatR;
using vet.Domain.Ports;

using ProductEntity = vet.Domain.Entities.Product;
using ServiceSupplyEntity = vet.Domain.Entities.ServiceSupply;

namespace vet.Application.UseCases.Product.Commands;

public class DeactivateProductCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeactivateProductCommandHandler
    : IRequestHandler<DeactivateProductCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeactivateProductCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
            throw new ArgumentException(
                "El identificador del producto no es válido.");

        // 1. Buscar producto

        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.Id);

        if (product is null)
            throw new KeyNotFoundException(
                "El producto no existe.");

        // 2. Verificar estado actual

        if (!product.IsActive)
            return true;

        // 3. Verificar asociaciones con servicios

        var supplies = await _unitOfWork
            .Repository<ServiceSupplyEntity>()
            .FindAsync(s => s.ProductId == product.Id);

        if (supplies.Any())
            throw new ArgumentException(
                "El producto está asociado a servicios. " +
                "Retire primero sus asociaciones.");

        // 4. Desactivar producto

        product.IsActive = false;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}