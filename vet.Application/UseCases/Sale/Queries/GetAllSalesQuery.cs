
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.Sale;

namespace vet.Application.UseCases.Sale.Queries;

public record GetAllSalesQuery(long CompanyId)
    : IRequest<List<SaleListResponse>>;

public class SaleListResponse
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long OwnerId { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public long? AppointmentId { get; set; }

    public DateTime Date { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ReceiptNumber { get; set; }
}

public class GetAllSalesQueryHandler
    : IRequestHandler<GetAllSalesQuery, List<SaleListResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllSalesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SaleListResponse>> Handle(
        GetAllSalesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId <= 0)
            throw new ArgumentException(
                "La empresa es obligatoria.");

        // 1. Obtener ventas
        var sales = (await _unitOfWork
            .Repository<Domain.Entities.Sale>()
            .FindAsync(s => s.CompanyId == request.CompanyId))
            .OrderByDescending(s => s.Date)
            .ToList();

        if (sales.Count == 0)
            return new List<SaleListResponse>();

        var saleIds = sales
            .Select(s => s.Id)
            .ToList();

        var ownerIds = sales
            .Select(s => s.OwnerId)
            .Distinct()
            .ToList();

        // 2. Obtener propietarios
        var owners = await _unitOfWork
            .Repository<Owner>()
            .FindAsync(o => ownerIds.Contains(o.Id));

        var ownersById = owners.ToDictionary(o => o.Id);

        // 3. Obtener detalles
        var items = await _unitOfWork
            .Repository<SaleItem>()
            .FindAsync(i => saleIds.Contains(i.SaleId));

        var itemsBySale = items.ToLookup(i => i.SaleId);

        // 4. Obtener recibos internos
        var invoices = await _unitOfWork
            .Repository<Invoice>()
            .FindAsync(i =>
                i.SaleId.HasValue &&
                saleIds.Contains(i.SaleId.Value) &&
                i.Type == "INTERNAL" &&
                i.Status == "ISSUED");

        var invoicesBySale = invoices
            .ToLookup(i => i.SaleId!.Value);

        // 5. Construir respuesta
        return sales.Select(sale =>
        {
            var owner = ownersById.GetValueOrDefault(
                sale.OwnerId);

            var invoice = invoicesBySale[sale.Id]
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();

            decimal total = itemsBySale[sale.Id]
                .Sum(item => SaleRules.CalculateItemTotal(
                    item.Quantity,
                    item.UnitPrice,
                    item.Discount,
                    item.Igv));

            return new SaleListResponse
            {
                Id = sale.Id,
                CompanyId = sale.CompanyId,
                OwnerId = sale.OwnerId,
                OwnerName = owner?.FirstNames ?? string.Empty,
                AppointmentId = sale.AppointmentId,
                Date = sale.Date,
                Total = total,
                Status = sale.Status,

                ReceiptNumber = invoice == null
                    ? null
                    : $"{invoice.Series}-{invoice.SequenceNumber:D6}"
            };

        }).ToList();
    }
}
