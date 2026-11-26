using MediatR;
using vet.Domain.Ports;

using InvoiceEntity = vet.Domain.Entities.Invoice;

namespace vet.Application.UseCases.Invoice.Queries;

public class GetAllInvoicesQuery
    : IRequest<List<InvoiceDto>>
{
    public long CompanyId { get; set; }
    public long? SaleId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
}

public class InvoiceDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long? SaleId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }

    public string DocumentNumber =>
        $"{Series}-{SequenceNumber:D6}";

    public string? CustomerDocType { get; set; }
    public string? CustomerDocNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public static InvoiceDto FromEntity(InvoiceEntity invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            CompanyId = invoice.CompanyId,
            SaleId = invoice.SaleId,
            Type = invoice.Type,
            Series = invoice.Series,
            SequenceNumber = invoice.SequenceNumber,
            CustomerDocType = invoice.CustomerDocType,
            CustomerDocNumber = invoice.CustomerDocNumber,
            CustomerName = invoice.CustomerName,
            Subtotal = invoice.Subtotal,
            Igv = invoice.Igv,
            Total = invoice.Total,
            Status = invoice.Status,
            Date = invoice.Date
        };
    }
}

public class GetAllInvoicesQueryHandler
    : IRequestHandler<GetAllInvoicesQuery, List<InvoiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllInvoicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<InvoiceDto>> Handle(
        GetAllInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId <= 0)
            throw new ArgumentException(
                "La empresa es obligatoria.");

        var type = string.IsNullOrWhiteSpace(request.Type)
            ? null
            : request.Type.Trim().ToUpperInvariant();

        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToUpperInvariant();

        if (type is not null &&
            type is not ("INTERNAL" or "RECEIPT" or "INVOICE"))
            throw new ArgumentException(
                "Tipo de comprobante inválido.");

        if (status is not null &&
            status is not (
                "DRAFT" or "ISSUED" or "ACCEPTED"
                or "REJECTED" or "VOIDED"))
            throw new ArgumentException(
                "Estado de comprobante inválido.");

        var invoices = await _unitOfWork
            .Repository<InvoiceEntity>()
            .FindAsync(x =>
                x.CompanyId == request.CompanyId &&
                (!request.SaleId.HasValue ||
                 x.SaleId == request.SaleId.Value) &&
                (type == null || x.Type == type) &&
                (status == null || x.Status == status));

        return invoices
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Select(InvoiceDto.FromEntity)
            .ToList();
    }
}