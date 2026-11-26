using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.ServiceSupply;

using ServiceEntity = vet.Domain.Entities.Service;
using ProductEntity = vet.Domain.Entities.Product;
using SupplyEntity = vet.Domain.Entities.ServiceSupply;

namespace vet.Application.UseCases.ServiceSupply.Commands;

public class AssignSupplyToServiceCommand : IRequest<bool>
{
    public long ServiceId { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class AssignSupplyToServiceCommandHandler
    : IRequestHandler<AssignSupplyToServiceCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignSupplyToServiceCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        AssignSupplyToServiceCommand request,
        CancellationToken cancellationToken)
    {
        ServiceSupplyRules.ValidateIdentifiers(
            request.ServiceId,
            request.ProductId);

        ServiceSupplyRules.ValidateQuantity(
            request.Quantity);

        // 1. Buscar servicio

        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.ServiceId);

        if (service is null)
            throw new KeyNotFoundException(
                "El servicio no existe.");

        // 2. Buscar producto

        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.ProductId);

        if (product is null)
            throw new KeyNotFoundException(
                "El producto no existe.");

        ServiceSupplyRules.ValidateActiveEntities(
            service.IsActive,
            product.IsActive);

        // 3. Verificar asignación duplicada

        var repository = _unitOfWork
            .Repository<SupplyEntity>();

        var existing = await repository.FindAsync(s =>
            s.ServiceId == request.ServiceId &&
            s.ProductId == request.ProductId);

        if (existing.Any())
            throw new ArgumentException(
                "El producto ya está asignado al servicio.");

        // 4. Registrar asignación

        var supply = new SupplyEntity
        {
            ServiceId = request.ServiceId,
            ProductId = request.ProductId,
            Quantity = request.Quantity
        };

        await repository.AddAsync(supply);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}