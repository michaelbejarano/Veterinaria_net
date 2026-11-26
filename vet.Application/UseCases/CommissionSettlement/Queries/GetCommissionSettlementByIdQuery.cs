using MediatR;
using vet.Domain.Ports;

using SettlementEntity = vet.Domain.Entities.CommissionSettlement;
using SettlementItemEntity = vet.Domain.Entities.SettlementItem;
using CommissionEntity = vet.Domain.Entities.Commission;

namespace vet.Application.UseCases.CommissionSettlement.Queries;

public class GetCommissionSettlementByIdQuery
    : IRequest<CommissionSettlementDetailDto>
{
    public long Id { get; set; }
}

public class CommissionSettlementDetailDto
    : CommissionSettlementDto
{
    public List<SettlementCommissionDto> Commissions
        { get; set; } = new();
}

public class SettlementCommissionDto
{
    public long CommissionId { get; set; }

    public long VisitServiceId { get; set; }

    public long? RuleId { get; set; }

    public string AppliedType { get; set; } = string.Empty;

    public decimal AppliedValue { get; set; }

    public decimal CalculationBase { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

public class GetCommissionSettlementByIdQueryHandler
    : IRequestHandler<
        GetCommissionSettlementByIdQuery,
        CommissionSettlementDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCommissionSettlementByIdQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CommissionSettlementDetailDto> Handle(
        GetCommissionSettlementByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener cabecera

        var settlement = await _unitOfWork
            .Repository<SettlementEntity>()
            .GetByIdAsync(request.Id);

        if (settlement is null)
            throw new KeyNotFoundException(
                "La liquidación no existe.");

        // 2. Obtener detalles

        var items = await _unitOfWork
            .Repository<SettlementItemEntity>()
            .FindAsync(i =>
                i.SettlementId == settlement.Id);

        var commissionIds = items
            .Select(i => i.CommissionId)
            .ToList();

        // 3. Obtener comisiones

        var commissions = (await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(c =>
                commissionIds.Contains(c.Id)))
            .ToList();

        // 4. Preparar respuesta

        return new CommissionSettlementDetailDto
        {
            Id = settlement.Id,
            EmployeeId = settlement.EmployeeId,
            Date = settlement.Date,
            Status = settlement.Status,
            PaidAt = settlement.PaidAt,

            TotalAmount = commissions.Sum(c => c.Amount),

            CommissionCount = commissions.Count,

            Commissions = commissions
                .OrderBy(c => c.Date)
                .ThenBy(c => c.Id)
                .Select(c => new SettlementCommissionDto
                {
                    CommissionId = c.Id,
                    VisitServiceId = c.VisitServiceId,
                    RuleId = c.RuleId,
                    AppliedType = c.AppliedType,
                    AppliedValue = c.AppliedValue,
                    CalculationBase = c.CalculationBase,
                    Amount = c.Amount,
                    Status = c.Status,
                    Date = c.Date
                })
                .ToList()
        };
    }
}