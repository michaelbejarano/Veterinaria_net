using MediatR;
using vet.Domain.Ports;

using ServiceEntity = vet.Domain.Entities.Service;
using ProductEntity = vet.Domain.Entities.Product;
using SupplyEntity = vet.Domain.Entities.ServiceSupply;

namespace vet.Application.UseCases.ServiceSupply.Queries;

public record GetServiceSuppliesQuery(long ServiceId)
    : IRequest<List<ServiceSupplyDto>>;

public class ServiceSupplyDto
{
    public long ServiceId { get; set; }
    public long ProductId { get; set; }
    public string? Sku { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public bool TrackBatches { get; set; }
    public bool IsActive { get; set; }
}

public class GetServiceSuppliesQueryHandler
    : IRequestHandler<
        GetServiceSuppliesQuery,
        List<ServiceSupplyDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServiceSuppliesQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceSupplyDto>> Handle(
        GetServiceSuppliesQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar servicio

        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.ServiceId);

        if (service is null)
            throw new KeyNotFoundException(
                "El servicio no existe.");

        // 2. Buscar insumos asignados

        var supplies = await _unitOfWork
            .Repository<SupplyEntity>()
            .FindAsync(s =>
                s.ServiceId == request.ServiceId);

        if (!supplies.Any())
            return new List<ServiceSupplyDto>();

        var productIds = supplies
            .Select(s => s.ProductId)
            .Distinct()
            .ToList();

        // 3. Buscar productos

        var products = await _unitOfWork
            .Repository<ProductEntity>()
            .FindAsync(p =>
                productIds.Contains(p.Id));

        var productDictionary = products
            .ToDictionary(p => p.Id);

        // 4. Construir respuesta

        return supplies
            .Select(s =>
            {
                var product =
                    productDictionary[s.ProductId];

                return new ServiceSupplyDto
                {
                    ServiceId = s.ServiceId,
                    ProductId = s.ProductId,
                    Sku = product.Sku,
                    ProductName = product.Name,
                    ProductType = product.Type,
                    Unit = product.Unit,
                    Quantity = s.Quantity,
                    TrackBatches = product.TrackBatches,
                    IsActive = product.IsActive
                };
            })
            .OrderBy(s => s.ProductName)
            .ToList();
    }
}