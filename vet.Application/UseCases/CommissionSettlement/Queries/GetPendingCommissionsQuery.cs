using MediatR;
using vet.Domain.Ports;

using CommissionEntity = vet.Domain.Entities.Commission;
using SettlementItemEntity = vet.Domain.Entities.SettlementItem;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.CommissionSettlement.Queries;

public class GetPendingCommissionsQuery
    : IRequest<List<PendingCommissionDto>>
{
    public long EmployeeId { get; set; }

    public DateOnly? UntilDate { get; set; }
}

public class PendingCommissionDto
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public long VisitServiceId { get; set; }

    public string AppliedType { get; set; } = string.Empty;

    public decimal AppliedValue { get; set; }

    public decimal CalculationBase { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public long? SettlementId { get; set; }

    public bool IsAvailableForSettlement { get; set; }
}

public class GetPendingCommissionsQueryHandler
    : IRequestHandler<
        GetPendingCommissionsQuery,
        List<PendingCommissionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingCommissionsQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PendingCommissionDto>> Handle(
        GetPendingCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar trabajador

        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null)
            throw new KeyNotFoundException(
                "El trabajador no existe.");

        // 2. Calcular límite de fecha

        var timeZone = TimeZoneInfo
            .FindSystemTimeZoneById("America/Lima");

        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, timeZone));

        var untilDate = request.UntilDate ?? today;

        if (untilDate == default || untilDate > today)
            throw new ArgumentException(
                "La fecha de consulta no es válida.");

        var localEnd = untilDate
            .AddDays(1)
            .ToDateTime(TimeOnly.MinValue);

        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            localEnd, timeZone);

        // 3. Obtener comisiones pendientes

        var commissions = (await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(c =>
                c.EmployeeId == request.EmployeeId &&
                c.Status == "PENDING" &&
                c.Date < endUtc))
            .ToList();

        var commissionIds = commissions
            .Select(c => c.Id)
            .ToList();

        // 4. Obtener liquidaciones asociadas

        var items = await _unitOfWork
            .Repository<SettlementItemEntity>()
            .FindAsync(i =>
                commissionIds.Contains(i.CommissionId));

        var assignedItems = items.ToDictionary(
            i => i.CommissionId,
            i => i.SettlementId);

        // 5. Preparar respuesta

        return commissions
            .OrderBy(c => c.Date)
            .ThenBy(c => c.Id)
            .Select(c => new PendingCommissionDto
            {
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                VisitServiceId = c.VisitServiceId,
                AppliedType = c.AppliedType,
                AppliedValue = c.AppliedValue,
                CalculationBase = c.CalculationBase,
                Amount = c.Amount,
                Date = c.Date,

                SettlementId = assignedItems.TryGetValue(
                    c.Id, out var settlementId)
                    ? settlementId
                    : null,

                IsAvailableForSettlement =
                    !assignedItems.ContainsKey(c.Id)
            })
            .ToList();
    }
}