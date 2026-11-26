using MediatR;
using vet.Domain.Ports;

using ServiceEntity = vet.Domain.Entities.Service;
using VisitEntity = vet.Domain.Entities.Visit;
using VisitServiceEntity = vet.Domain.Entities.VisitService;
using SaleEntity = vet.Domain.Entities.Sale;
using SaleItemEntity = vet.Domain.Entities.SaleItem;

namespace vet.Application.UseCases.Report.Queries;

public class GetTopServicesQuery : IRequest<List<TopServiceDto>>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int Limit { get; set; } = 10;
}

public class TopServiceDto
{
    public long ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    public int CompletedCount { get; set; }
    public int BilledCount { get; set; }

    public decimal CompletedValue { get; set; }
    public decimal BilledAmount { get; set; }
}

public class GetTopServicesQueryHandler
    : IRequestHandler<GetTopServicesQuery, List<TopServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTopServicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TopServiceDto>> Handle(
        GetTopServicesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Limit < 1 || request.Limit > 100)
            throw new ArgumentException(
                "El límite debe estar entre 1 y 100.");

        var (start, end) = ReportHelper.GetRange(
            request.FromDate, request.ToDate);

        var visits = await _unitOfWork
            .Repository<VisitEntity>()
            .FindAsync(x =>
                x.StartAt >= start &&
                x.StartAt < end &&
                x.Status != "VOIDED");

        var visitIds = visits.Select(x => x.Id).ToList();

        var visitServices = await _unitOfWork
            .Repository<VisitServiceEntity>()
            .FindAsync(x =>
                visitIds.Contains(x.VisitId) &&
                x.Status == "COMPLETED");

        var serviceIds = visitServices
            .Select(x => x.ServiceId).Distinct().ToList();

        var visitServiceIds = visitServices
            .Select(x => x.Id).ToList();

        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(x => serviceIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var saleItems = await _unitOfWork
            .Repository<SaleItemEntity>()
            .FindAsync(x =>
                x.VisitServiceId.HasValue &&
                visitServiceIds.Contains(x.VisitServiceId.Value));

        var saleIds = saleItems
            .Select(x => x.SaleId).Distinct().ToList();

        var confirmedSales = await _unitOfWork
            .Repository<SaleEntity>()
            .FindAsync(x =>
                saleIds.Contains(x.Id) &&
                x.Status == "CONFIRMED");

        var confirmedIds = confirmedSales
            .Select(x => x.Id).ToHashSet();

        var billedItems = saleItems
            .Where(x => confirmedIds.Contains(x.SaleId))
            .ToLookup(x => x.VisitServiceId!.Value);

        return visitServices
            .GroupBy(x => x.ServiceId)
            .Select(g =>
            {
                var items = g
                    .SelectMany(x => billedItems[x.Id])
                    .ToList();

                return new TopServiceDto
                {
                    ServiceId = g.Key,

                    ServiceName = services[g.Key].Name,

                    CompletedCount = g.Count(),

                    CompletedValue = g.Sum(x => x.FinalPrice),

                    BilledCount = items
                        .Select(x => x.VisitServiceId)
                        .Distinct().Count(),

                    BilledAmount = items.Sum(ReportHelper.ItemTotal)
                };
            })
            .OrderByDescending(x => x.CompletedCount)
            .ThenByDescending(x => x.BilledAmount)
            .Take(request.Limit)
            .ToList();
    }
}