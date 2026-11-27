
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;
using vet.Domain.Services.Sale;

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

    public decimal AvailableDeposit { get; set; }

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
        // ========================================
        // 1. VALIDAR IDENTIFICADOR
        // ========================================

        if (request.SaleId <= 0)
            throw new ArgumentException(
                "La venta indicada no es válida.");

        // ========================================
        // 2. OBTENER VENTA
        // ========================================

        var sale = await _unitOfWork
            .Repository<Domain.Entities.Sale>()
            .GetByIdAsync(request.SaleId);

        if (sale == null)
            throw new KeyNotFoundException(
                "La venta no existe.");

        // ========================================
        // 3. OBTENER PROPIETARIO
        // ========================================

        var owner = await _unitOfWork
            .Repository<Domain.Entities.Owner>()
            .GetByIdAsync(sale.OwnerId);

        // ========================================
        // 4. OBTENER DETALLES
        // ========================================

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

        // ========================================
        // 5. OBTENER PAGOS DE LA VENTA
        // ========================================

        var payments = await _unitOfWork
            .Repository<Payment>()
            .FindAsync(p => p.SaleId == sale.Id);

        var paymentResponses = payments
            .OrderBy(p => p.Date)
            .ThenBy(p => p.Id)
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

        // ========================================
        // 6. OBTENER ADELANTOS YA APLICADOS
        // ========================================

        var appliedDeposits = await _unitOfWork
            .Repository<DepositApplication>()
            .FindAsync(d => d.SaleId == sale.Id);

        decimal appliedDeposit = appliedDeposits
            .Sum(d => d.Amount);

        // ========================================
        // 7. CALCULAR SALDO ACTUAL
        // ========================================

        decimal remainingBalance = sale.Status == "VOIDED"
            ? 0
            : Math.Max(
                0,
                total - appliedDeposit - amountPaid);

        // ========================================
        // 8. CONSULTAR ADELANTOS DISPONIBLES
        // ========================================

        decimal availableDeposit = 0;

        if (sale.Status == "OPEN" &&
            sale.AppointmentId.HasValue &&
            itemResponses.Any(i => i.VisitServiceId.HasValue) &&
            remainingBalance > 0)
        {
            var appointment = await _unitOfWork
                .Repository<Domain.Entities.Appointment>()
                .GetByIdAsync(sale.AppointmentId.Value);

            if (appointment != null &&
                appointment.Status is ("IN_PROGRESS" or "COMPLETED"))
            {
                // Obtener únicamente pagos confirmados
                // correspondientes a la cita.

                var deposits = (await _unitOfWork
                    .Repository<Payment>()
                    .FindAsync(p =>
                        p.AppointmentId == sale.AppointmentId &&
                        p.Status == "CONFIRMED"))
                    .ToList();

                if (deposits.Count > 0)
                {
                    var depositIds = deposits
                        .Select(p => p.Id)
                        .ToList();

                    // Obtener adelantos retenidos.

                    var forfeitures = await _unitOfWork
                        .Repository<DepositForfeiture>()
                        .FindAsync(f =>
                            depositIds.Contains(f.PaymentId));

                    var forfeitedPaymentIds = forfeitures
                        .Select(f => f.PaymentId)
                        .ToHashSet();

                    // Obtener aplicaciones anteriores.

                    var applications = await _unitOfWork
                        .Repository<DepositApplication>()
                        .FindAsync(a =>
                            depositIds.Contains(a.PaymentId));

                    var applicationsByPayment = applications
                        .ToLookup(a => a.PaymentId);

                    foreach (var deposit in deposits.OrderBy(p => p.Id))
                    {
                        // No considerar adelantos retenidos.

                        if (forfeitedPaymentIds.Contains(deposit.Id))
                            continue;

                        decimal alreadyApplied =
                            applicationsByPayment[deposit.Id]
                                .Sum(a => a.Amount);

                        decimal availableAmount = Math.Max(
                            0,
                            deposit.Amount - alreadyApplied);

                        if (availableAmount <= 0)
                            continue;

                        decimal amountToConsider = Math.Min(
                            availableAmount,
                            remainingBalance - availableDeposit);

                        if (amountToConsider <= 0)
                            break;

                        availableDeposit += amountToConsider;

                        if (availableDeposit >= remainingBalance)
                            break;
                    }
                }
            }
        }

        // Descontar el adelanto disponible únicamente
        // para mostrar el importe estimado por cobrar.

        remainingBalance = SaleRules.CalculateRemainingBalance(
            remainingBalance,
            availableDeposit);

        // ========================================
        // 9. OBTENER RECIBO INTERNO
        // ========================================

        var invoices = await _unitOfWork
            .Repository<Domain.Entities.Invoice>()
            .FindAsync(i =>
                i.SaleId == sale.Id &&
                i.Type == "INTERNAL");

        var invoice = invoices
            .OrderByDescending(i => i.Id)
            .FirstOrDefault();

        // ========================================
        // 10. CONSTRUIR RESPUESTA
        // ========================================

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

            AvailableDeposit = availableDeposit,

            AppliedDeposit = appliedDeposit,

            AmountPaid = amountPaid,

            RemainingBalance = remainingBalance,

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
