using MediatR;
using vet.Domain.Ports;

using SettlementEntity = vet.Domain.Entities.CommissionSettlement;
using SettlementItemEntity = vet.Domain.Entities.SettlementItem;
using CommissionEntity = vet.Domain.Entities.Commission;

namespace vet.Application.UseCases.CommissionSettlement.Queries;

public class GetAllCommissionSettlementsQuery
    : IRequest<List<CommissionSettlementDto>>
{
    public long? EmployeeId { get; set; }

    public string? Status { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }
}

public class CommissionSettlementDto
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }

    public decimal TotalAmount { get; set; }

    public int CommissionCount { get; set; }
}

public class GetAllCommissionSettlementsQueryHandler
    : IRequestHandler<
        GetAllCommissionSettlementsQuery,
        List<CommissionSettlementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCommissionSettlementsQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CommissionSettlementDto>> Handle(
        GetAllCommissionSettlementsQuery request,
        CancellationToken cancellationToken)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToUpperInvariant();

        if (status != null &&
            status is not ("PENDING" or "PAID" or "VOIDED"))
        {
            throw new ArgumentException(
                "El estado de liquidación no es válido.");
        }

        if (request.FromDate.HasValue &&
            request.ToDate.HasValue &&
            request.FromDate.Value > request.ToDate.Value)
        {
            throw new ArgumentException(
                "El rango de fechas no es válido.");
        }

        // 1. Obtener liquidaciones

        var settlements = (await _unitOfWork
            .Repository<SettlementEntity>()
            .FindAsync(s =>
                (!request.EmployeeId.HasValue ||
                 s.EmployeeId == request.EmployeeId.Value) &&

                (status == null || s.Status == status) &&

                (!request.FromDate.HasValue ||
                 s.Date >= request.FromDate.Value) &&

                (!request.ToDate.HasValue ||
                 s.Date <= request.ToDate.Value)))
            .ToList();

        if (settlements.Count == 0)
            return new List<CommissionSettlementDto>();

        var settlementIds = settlements
            .Select(s => s.Id)
            .ToList();

        // 2. Obtener detalles

        var items = (await _unitOfWork
            .Repository<SettlementItemEntity>()
            .FindAsync(i =>
                settlementIds.Contains(i.SettlementId)))
            .ToList();

        var commissionIds = items
            .Select(i => i.CommissionId)
            .Distinct()
            .ToList();

        // 3. Obtener comisiones

        var commissions = await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(c =>
                commissionIds.Contains(c.Id));

        var amounts = commissions.ToDictionary(
            c => c.Id,
            c => c.Amount);

        var itemsBySettlement = items.ToLookup(
            i => i.SettlementId);

        // 4. Preparar respuesta

        return settlements
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .Select(s => new CommissionSettlementDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                Date = s.Date,
                Status = s.Status,
                PaidAt = s.PaidAt,

                TotalAmount = itemsBySettlement[s.Id]
                    .Sum(i => amounts[i.CommissionId]),

                CommissionCount =
                    itemsBySettlement[s.Id].Count()
            })
            .ToList();
    }
}