
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.Sale;

namespace vet.Application.UseCases.Sale.Commands;

// ========================================
// COMMAND
// ========================================
public class IssueInternalReceiptCommand
    : IRequest<IssueInternalReceiptResponse>
{
    public long SaleId { get; set; }
}

// ========================================
// RESPONSE
// ========================================
public class IssueInternalReceiptResponse
{
    public long InvoiceId { get; set; }

    public long SaleId { get; set; }

    public string Series { get; set; } = string.Empty;

    public long SequenceNumber { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

// ========================================
// HANDLER
// ========================================
public class IssueInternalReceiptCommandHandler
    : IRequestHandler<
        IssueInternalReceiptCommand,
        IssueInternalReceiptResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public IssueInternalReceiptCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IssueInternalReceiptResponse> Handle(
        IssueInternalReceiptCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SaleId <= 0)
            throw new ArgumentException(
                "La venta es obligatoria.");

        IssueInternalReceiptResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear venta
                var sale = await _unitOfWork
                    .GetSaleForUpdateAsync(
                        request.SaleId,
                        ct);

                if (sale == null)
                    throw new ArgumentException(
                        "La venta no existe.");

                // 2. Validar estado de la venta
                SaleRules.ValidateReceiptIssuance(
                    sale.Status);

                // 3. Evitar recibos duplicados
                var existingInvoices = await _unitOfWork
                    .Repository<Domain.Entities.Invoice>()
                    .FindAsync(i =>
                        i.SaleId == sale.Id &&
                        i.Type == "INTERNAL");

                if (existingInvoices.Any())
                    throw new ArgumentException(
                        "La venta ya tiene un recibo interno.");

                // 4. Obtener propietario
                var owner = await _unitOfWork
                    .Repository<Domain.Entities.Owner>()
                    .GetByIdAsync(sale.OwnerId);

                if (owner == null)
                    throw new ArgumentException(
                        "El propietario no existe.");

                // 5. Obtener detalles de venta
                var saleItems = (await _unitOfWork
                    .Repository<SaleItem>()
                    .FindAsync(i => i.SaleId == sale.Id))
                    .ToList();

                SaleRules.ValidateSale(
                    sale.OwnerId,
                    saleItems.Count);

                // 6. Calcular importes
                decimal total = saleItems.Sum(item =>
                    SaleRules.CalculateItemTotal(
                        item.Quantity,
                        item.UnitPrice,
                        item.Discount,
                        item.Igv));

                decimal igv = saleItems.Sum(i => i.Igv);

                decimal subtotal = total - igv;

                // 7. Generar correlativo
                const string series = "RI01";

                long sequenceNumber = await _unitOfWork
                    .GetNextInternalReceiptNumberAsync(
                        sale.CompanyId,
                        series,
                        ct);

                // 8. Crear recibo interno
                var invoice = new Domain.Entities.Invoice
                {
                    CompanyId = sale.CompanyId,
                    SaleId = sale.Id,

                    Type = "INTERNAL",
                    Series = series,
                    SequenceNumber = sequenceNumber,

                    CustomerName = owner.FirstNames,
                    CustomerDocType = owner.DocumentType,
                    CustomerDocNumber = owner.DocumentNumber,

                    Subtotal = subtotal,
                    Igv = igv,
                    Total = total,

                    Status = "ISSUED",
                    Date = DateTime.UtcNow
                };

                await _unitOfWork
                    .Repository<Domain.Entities.Invoice>()
                    .AddAsync(invoice);

                await _unitOfWork.SaveChangesAsync(ct);

                // 9. Preparar respuesta
                response = new IssueInternalReceiptResponse
                {
                    InvoiceId = invoice.Id,
                    SaleId = sale.Id,
                    Series = invoice.Series,
                    SequenceNumber = invoice.SequenceNumber,

                    ReceiptNumber =
                        $"{invoice.Series}-{invoice.SequenceNumber:D6}",

                    Subtotal = invoice.Subtotal,
                    Igv = invoice.Igv,
                    Total = invoice.Total,
                    Status = invoice.Status,
                    Date = invoice.Date
                };
            },
            cancellationToken);

        return response!;
    }
}
