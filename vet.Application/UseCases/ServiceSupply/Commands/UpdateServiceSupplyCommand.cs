using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.ServiceSupply;

using ServiceEntity = vet.Domain.Entities.Service;
using ProductEntity = vet.Domain.Entities.Product;
using SupplyEntity = vet.Domain.Entities.ServiceSupply;

namespace vet.Application.UseCases.ServiceSupply.Commands;

public class UpdateServiceSupplyCommand : IRequest<bool>
{
    public long ServiceId { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class UpdateServiceSupplyCommandHandler
    : IRequestHandler<UpdateServiceSupplyCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceSupplyCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UpdateServiceSupplyCommand request,
        CancellationToken cancellationToken)
    {
        ServiceSupplyRules.ValidateIdentifiers(
            request.ServiceId,
            request.ProductId);

        ServiceSupplyRules.ValidateQuantity(
            request.Quantity);

        // 1. Buscar asignación

        var repository = _unitOfWork
            .Repository<SupplyEntity>();

        var supplies = await repository.FindAsync(s =>
            s.ServiceId == request.ServiceId &&
            s.ProductId == request.ProductId);

        var supply = supplies.SingleOrDefault();

        if (supply is null)
            throw new KeyNotFoundException(
                "El insumo no está asignado al servicio.");

        // 2. Verificar servicio y producto

        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.ServiceId);

        if (service is null)
            throw new KeyNotFoundException(
                "El servicio no existe.");

        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.ProductId);

        if (product is null)
            throw new KeyNotFoundException(
                "El producto no existe.");

        ServiceSupplyRules.ValidateActiveEntities(
            service.IsActive,
            product.IsActive);

        // 3. Actualizar cantidad

        supply.Quantity = request.Quantity;

        repository.Update(supply);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}