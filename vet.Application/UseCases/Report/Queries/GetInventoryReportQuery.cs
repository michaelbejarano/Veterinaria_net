
using MediatR;
using vet.Domain.Ports;

using SaleEntity = vet.Domain.Entities.Sale;
using SaleItemEntity = vet.Domain.Entities.SaleItem;
using ProductEntity = vet.Domain.Entities.Product;

namespace vet.Application.UseCases.Report.Queries;

public class GetInventoryReportQuery
    : IRequest<InventoryReportDto>
{
    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public int Limit { get; set; } = 10;

    public long? CompanyId { get; set; }
}

public class InventoryReportDto
{
    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public int TotalProductsSold { get; set; }

    public decimal TotalUnitsSold { get; set; }

    public decimal TotalAmount { get; set; }

    public List<TopSellingProductDto> TopProducts { get; set; }
        = new();
}

public class TopSellingProductDto
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? Sku { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal QuantitySold { get; set; }

    public decimal TotalAmount { get; set; }
}

public class GetInventoryReportQueryHandler
    : IRequestHandler<GetInventoryReportQuery, InventoryReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInventoryReportQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InventoryReportDto> Handle(
        GetInventoryReportQuery request,
        CancellationToken cancellationToken)
    {
        var today = ReportHelper.Today();

        var fromDate = request.FromDate ??
            new DateOnly(today.Year, today.Month, 1);

        var toDate = request.ToDate ?? today;

        if (request.Limit < 1 || request.Limit > 100)
        {
            throw new ArgumentException(
                "El límite debe estar entre 1 y 100.");
        }

        if (request.CompanyId.HasValue &&
            request.CompanyId.Value <= 0)
        {
            throw new ArgumentException(
                "La empresa seleccionada no es válida.");
        }

        var (start, end) = ReportHelper.GetRange(
            fromDate,
            toDate
        );

        var report = new InventoryReportDto
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        // Obtener únicamente ventas confirmadas.

        var sales = (await _unitOfWork
            .Repository<SaleEntity>()
            .FindAsync(s =>
                s.Status == "CONFIRMED" &&
                s.Date >= start &&
                s.Date < end &&
                (!request.CompanyId.HasValue ||
                 s.CompanyId == request.CompanyId.Value)
            )).ToList();

        if (sales.Count == 0)
        {
            return report;
        }

        var saleIds = sales
            .Select(s => s.Id)
            .ToList();

        // Obtener únicamente líneas con productos.

        var items = (await _unitOfWork
            .Repository<SaleItemEntity>()
            .FindAsync(i =>
                saleIds.Contains(i.SaleId) &&
                i.ProductId.HasValue
            )).ToList();

        if (items.Count == 0)
        {
            return report;
        }

        var productIds = items
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        // Recuperar los nombres de los productos.

        var products = (await _unitOfWork
            .Repository<ProductEntity>()
            .FindAsync(p =>
                productIds.Contains(p.Id)
            ))
            .ToDictionary(p => p.Id);

        // Agrupar ventas por producto.

        var groupedProducts = items
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .Select(group =>
            {
                products.TryGetValue(
                    group.Key,
                    out var product
                );

                return new TopSellingProductDto
                {
                    ProductId = group.Key,

                    ProductName = product?.Name ??
                        $"Producto #{group.Key}",

                    Sku = product?.Sku,

                    Unit = product?.Unit ?? "unidad",

                    QuantitySold = group.Sum(
                        i => i.Quantity
                    ),

                    TotalAmount = group.Sum(
                        i => ReportHelper.ItemTotal(i)
                    )
                };
            })
            .OrderByDescending(p => p.QuantitySold)
            .ThenByDescending(p => p.TotalAmount)
            .ThenBy(p => p.ProductName)
            .ToList();

        report.TotalProductsSold = groupedProducts.Count;

        report.TotalUnitsSold = groupedProducts.Sum(
            p => p.QuantitySold
        );

        report.TotalAmount = groupedProducts.Sum(
            p => p.TotalAmount
        );

        report.TopProducts = groupedProducts
            .Take(request.Limit)
            .ToList();

        return report;
    }
}
