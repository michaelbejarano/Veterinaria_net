
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.Sale;

namespace vet.Application.UseCases.Sale.Commands;

public class CreateSaleCommand : IRequest<CreateSaleResponse>
{
    public long CompanyId { get; set; }

    public long OwnerId { get; set; }

    public long? AppointmentId { get; set; }

    public string? Notes { get; set; }

    public List<CreateSaleItemRequest> Items { get; set; } = new();
}

public class CreateSaleItemRequest
{
    public long? ProductId { get; set; }

    public long? VisitServiceId { get; set; }

    public decimal Quantity { get; set; }
}

public class CreateSaleResponse
{
    public long SaleId { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class CreateSaleCommandHandler
    : IRequestHandler<CreateSaleCommand, CreateSaleResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSaleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateSaleResponse> Handle(
        CreateSaleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items == null)
            throw new ArgumentException(
                "Los detalles son obligatorios.");

        SaleRules.ValidateSale(
            request.OwnerId,
            request.Items.Count);

        CreateSaleResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Validar empresa
                var company = await _unitOfWork
                    .Repository<Company>()
                    .GetByIdAsync(request.CompanyId);

                if (company == null)
                    throw new ArgumentException(
                        "La empresa no existe.");

                // 2. Validar propietario
                var owner = await _unitOfWork
                    .Repository<Domain.Entities.Owner>()
                    .GetByIdAsync(request.OwnerId);

                if (owner == null)
                    throw new ArgumentException(
                        "El propietario no existe.");

                // 3. Validar cita cuando corresponda
                if (request.AppointmentId.HasValue)
                {
                    var appointment = await _unitOfWork
                        .GetAppointmentForUpdateAsync(
                            request.AppointmentId.Value,
                            ct);

                    if (appointment == null)
                        throw new ArgumentException(
                            "La cita no existe.");

                    var pet = await _unitOfWork
                        .Repository<Domain.Entities.Pet>()
                        .GetByIdAsync(appointment.PetId);

                    if (pet == null ||
                        pet.OwnerId != request.OwnerId)
                    {
                        throw new ArgumentException(
                            "La cita no pertenece al propietario.");
                    }

                    if (appointment.Status is
                        "CANCELADA" or "EXPIRADA" or "NO_ASISTIO")
                    {
                        throw new ArgumentException(
                            "La cita no permite registrar una venta.");
                    }
                }

                // 4. Preparar detalles de la venta
                var saleItems = new List<SaleItem>();

                var registeredServices = new HashSet<long>();

                decimal total = 0;

                foreach (var item in request.Items)
                {
                    bool isProduct = item.ProductId.HasValue;
                    bool isService = item.VisitServiceId.HasValue;

                    if (isProduct == isService)
                        throw new ArgumentException(
                            "El detalle debe contener un producto o un servicio.");

                    if (item.Quantity <= 0)
                        throw new ArgumentException(
                            "La cantidad debe ser mayor que cero.");

                    var saleItem = new SaleItem
                    {
                        Quantity = item.Quantity,
                        Discount = 0,
                        Igv = 0
                    };

                    // 5. Preparar producto
                    if (isProduct)
                    {
                        var product = await _unitOfWork
                            .Repository<Domain.Entities.Product>()
                            .GetByIdAsync(item.ProductId!.Value);

                        if (product == null || !product.IsActive)
                            throw new ArgumentException(
                                "El producto no existe o está inactivo.");

                        saleItem.ProductId = product.Id;
                        saleItem.Description = product.Name;
                        saleItem.UnitPrice = product.SalePrice;
                    }
                    else
                    {
                        // 6. Preparar servicio realizado
                        if (item.Quantity != 1)
                            throw new ArgumentException(
                                "La cantidad de un servicio realizado debe ser uno.");

                        long visitServiceId =
                            item.VisitServiceId!.Value;

                        if (!registeredServices.Add(visitServiceId))
                            throw new ArgumentException(
                                "El servicio está repetido en la venta.");

                        var visitService = await _unitOfWork
                            .Repository<Domain.Entities.VisitService>()
                            .GetByIdAsync(visitServiceId);

                        if (visitService == null)
                            throw new ArgumentException(
                                "El servicio realizado no existe.");

                        if (visitService.Status != "COMPLETED")
                            throw new ArgumentException(
                                "El servicio todavía no está completado.");

                        var visit = await _unitOfWork
                            .Repository<Visit>()
                            .GetByIdAsync(visitService.VisitId);

                        if (visit == null)
                            throw new ArgumentException(
                                "La atención no existe.");

                        var pet = await _unitOfWork
                            .Repository<Domain.Entities.Pet>()
                            .GetByIdAsync(visit.PetId);

                        if (pet == null ||
                            pet.OwnerId != request.OwnerId)
                        {
                            throw new ArgumentException(
                                "El servicio no pertenece al propietario.");
                        }

                        if (visit.AppointmentId != request.AppointmentId)
                            throw new ArgumentException(
                                "El servicio no corresponde a la cita indicada.");

                        var existingItems = await _unitOfWork
                            .Repository<SaleItem>()
                            .FindAsync(s =>
                                s.VisitServiceId == visitServiceId);

                        if (existingItems.Any())
                            throw new ArgumentException(
                                "El servicio ya está asociado a una venta.");

                        var service = await _unitOfWork
                            .Repository<Domain.Entities.Service>()
                            .GetByIdAsync(visitService.ServiceId);

                        if (service == null)
                            throw new ArgumentException(
                                "El servicio no existe.");

                        saleItem.VisitServiceId = visitService.Id;
                        saleItem.Description = service.Name;
                        saleItem.UnitPrice = visitService.FinalPrice;
                    }

                    // 7. Calcular importe
                    total += SaleRules.CalculateItemTotal(
                        saleItem.Quantity,
                        saleItem.UnitPrice,
                        saleItem.Discount,
                        saleItem.Igv);

                    saleItems.Add(saleItem);
                }

                // 8. Crear venta
                var sale = new Domain.Entities.Sale
                {
                    CompanyId = request.CompanyId,
                    OwnerId = request.OwnerId,
                    AppointmentId = request.AppointmentId,
                    Date = DateTime.UtcNow,
                    Status = "OPEN",
                    Notes = request.Notes,
                    SaleItems = saleItems
                };

                await _unitOfWork
                    .Repository<Domain.Entities.Sale>()
                    .AddAsync(sale);

                await _unitOfWork.SaveChangesAsync(ct);

                // 9. Preparar respuesta
                response = new CreateSaleResponse
                {
                    SaleId = sale.Id,
                    Total = total,
                    Status = sale.Status
                };
            },
            cancellationToken);

        return response!;
    }
}
