using MediatR;
using vet.Domain.Ports;

using SaleEntity = vet.Domain.Entities.Sale;
using SaleItemEntity = vet.Domain.Entities.SaleItem;
using OwnerEntity = vet.Domain.Entities.Owner;
using PaymentEntity = vet.Domain.Entities.Payment;
using DepositEntity = vet.Domain.Entities.DepositApplication;

namespace vet.Application.UseCases.Report.Queries;

public class GetSalesReportQuery : IRequest<SalesReportDto>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? CompanyId { get; set; }
}

public class SalesReportDto
{
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalIgv { get; set; }

    public decimal ProductSales { get; set; }
    public decimal ServiceSales { get; set; }

    public decimal AppliedDeposits { get; set; }
    public decimal DirectPayments { get; set; }

    public List<SaleReportItemDto> Sales { get; set; } = new();
}

public class SaleReportItemDto
{
    public long Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public decimal Total { get; set; }
    public decimal AppliedDeposit { get; set; }
    public decimal PaidAmount { get; set; }

    public decimal Balance => Total - AppliedDeposit - PaidAmount;
}

public class GetSalesReportQueryHandler
    : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSalesReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SalesReportDto> Handle(
        GetSalesReportQuery request,
        CancellationToken cancellationToken)
    {
        var (start, end) = ReportHelper.GetRange(
            request.FromDate, request.ToDate);

        var sales = await _unitOfWork
            .Repository<SaleEntity>()
            .FindAsync(x =>
                x.Status == "CONFIRMED" &&
                x.Date >= start &&
                x.Date < end &&
                (!request.CompanyId.HasValue ||
                 x.CompanyId == request.CompanyId.Value));

        var saleIds = sales.Select(x => x.Id).ToList();

        var ownerIds = sales
            .Select(x => x.OwnerId).Distinct().ToList();

        var items = await _unitOfWork
            .Repository<SaleItemEntity>()
            .FindAsync(x => saleIds.Contains(x.SaleId));

        var payments = await _unitOfWork
            .Repository<PaymentEntity>()
            .FindAsync(x =>
                x.SaleId.HasValue &&
                saleIds.Contains(x.SaleId.Value) &&
                x.Status == "CONFIRMED");

        var deposits = await _unitOfWork
            .Repository<DepositEntity>()
            .FindAsync(x => saleIds.Contains(x.SaleId));

        var owners = (await _unitOfWork
            .Repository<OwnerEntity>()
            .FindAsync(x => ownerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var itemsBySale = items.ToLookup(x => x.SaleId);

        var paymentsBySale = payments
            .ToLookup(x => x.SaleId!.Value);

        var depositsBySale = deposits.ToLookup(x => x.SaleId);

        return new SalesReportDto
        {
            TotalSales = sales.Count(),

            TotalAmount = items.Sum(ReportHelper.ItemTotal),

            TotalDiscount = items.Sum(x => x.Discount),

            TotalIgv = items.Sum(x => x.Igv),

            ProductSales = items
                .Where(x => x.ProductId.HasValue)
                .Sum(ReportHelper.ItemTotal),

            ServiceSales = items
                .Where(x => x.VisitServiceId.HasValue)
                .Sum(ReportHelper.ItemTotal),

            AppliedDeposits = deposits.Sum(x => x.Amount),

            DirectPayments = payments.Sum(x => x.Amount),

            Sales = sales
                .OrderByDescending(x => x.Date)
                .Select(x => new SaleReportItemDto
                {
                    Id = x.Id,
                    Customer = owners[x.OwnerId].FirstNames,
                    Date = x.Date,

                    Total = itemsBySale[x.Id]
                        .Sum(ReportHelper.ItemTotal),

                    AppliedDeposit = depositsBySale[x.Id]
                        .Sum(d => d.Amount),

                    PaidAmount = paymentsBySale[x.Id]
                        .Sum(p => p.Amount)

                }).ToList()
        };
    }
}