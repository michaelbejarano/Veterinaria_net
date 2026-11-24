
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services;

namespace vet.Application.UseCases.Sale.Queries;

// ========================================
// QUERY
// ========================================
public record GetSaleByIdQuery(long SaleId)
    : IRequest<SaleDetailResponse>;

// ========================================
// RESPONSE
// ========================================
public class SaleDetailResponse
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long OwnerId { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public long? AppointmentId { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public decimal Total { get; set; }

    public decimal AppliedDeposit { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal RemainingBalance { get; set; }

    public List<SaleItemResponse> Items { get; set; } = new();

    public List<SalePaymentResponse> Payments { get; set; } = new();

    public SaleReceiptResponse? Receipt { get; set; }
}

// ========================================
// DETALLES
// ========================================
public class SaleItemResponse
{
    public long Id { get; set; }

    public long? ProductId { get; set; }

    public long? VisitServiceId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }
}

// ========================================
// PAGOS
// ========================================
public class SalePaymentResponse
{
    public long Id { get; set; }

    public string Method { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Reference { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = string.Empty;
}

// ========================================
// RECIBO
// ========================================
public class SaleReceiptResponse
{
    public long InvoiceId { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

// ========================================
// HANDLER
// ========================================
public class GetSaleByIdQueryHandler
    : IRequestHandler<GetSaleByIdQuery, SaleDetailResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSaleByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SaleDetailResponse> Handle(
        GetSaleByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener venta
        var sale = await _unitOfWork
            .Repository<Domain.Entities.Sale>()
            .GetByIdAsync(request.SaleId);

        if (sale == null)
            throw new ArgumentException(
                "La venta no existe.");

        // 2. Obtener propietario
        var owner = await _unitOfWork
            .Repository<Owner>()
            .GetByIdAsync(sale.OwnerId);

        // 3. Obtener detalles
        var items = await _unitOfWork
            .Repository<SaleItem>()
            .FindAsync(i => i.SaleId == sale.Id);

        var itemResponses = items
            .OrderBy(i => i.Id)
            .Select(item => new SaleItemResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                VisitServiceId = item.VisitServiceId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                Igv = item.Igv,

                Total = SaleRules.CalculateItemTotal(
                    item.Quantity,
                    item.UnitPrice,
                    item.Discount,
                    item.Igv)
            })
            .ToList();

        decimal total = itemResponses.Sum(i => i.Total);

        // 4. Obtener pagos
        var payments = await _unitOfWork
            .Repository<Payment>()
            .FindAsync(p => p.SaleId == sale.Id);

        var paymentResponses = payments
            .OrderBy(p => p.Date)
            .Select(p => new SalePaymentResponse
            {
                Id = p.Id,
                Method = p.Method,
                Amount = p.Amount,
                Reference = p.Reference,
                Date = p.Date,
                Status = p.Status
            })
            .ToList();

        decimal amountPaid = payments
            .Where(p => p.Status == "CONFIRMED")
            .Sum(p => p.Amount);

        // 5. Obtener adelantos aplicados
        var deposits = await _unitOfWork
            .Repository<DepositApplication>()
            .FindAsync(d => d.SaleId == sale.Id);

        decimal appliedDeposit = deposits.Sum(d => d.Amount);

        // 6. Obtener recibo interno
        var invoices = await _unitOfWork
            .Repository<Invoice>()
            .FindAsync(i =>
                i.SaleId == sale.Id &&
                i.Type == "INTERNAL");

        var invoice = invoices
            .OrderByDescending(i => i.Id)
            .FirstOrDefault();

        // 7. Construir respuesta
        return new SaleDetailResponse
        {
            Id = sale.Id,
            CompanyId = sale.CompanyId,
            OwnerId = sale.OwnerId,
            OwnerName = owner?.FirstNames ?? string.Empty,
            AppointmentId = sale.AppointmentId,
            Date = sale.Date,
            Status = sale.Status,
            Notes = sale.Notes,

            Total = total,
            AppliedDeposit = appliedDeposit,
            AmountPaid = amountPaid,

            RemainingBalance = sale.Status == "VOIDED"
                ? 0
                : Math.Max(
                    0,
                    total - appliedDeposit - amountPaid),

            Items = itemResponses,
            Payments = paymentResponses,

            Receipt = invoice == null
                ? null
                : new SaleReceiptResponse
                {
                    InvoiceId = invoice.Id,

                    ReceiptNumber =
                        $"{invoice.Series}-{invoice.SequenceNumber:D6}",

                    Total = invoice.Total,
                    Status = invoice.Status,
                    Date = invoice.Date
                }
        };
    }
}
