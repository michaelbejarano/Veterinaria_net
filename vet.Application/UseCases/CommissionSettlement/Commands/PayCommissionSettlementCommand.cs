using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.Commission;

using CommissionEntity = vet.Domain.Entities.Commission;
using SettlementItemEntity = vet.Domain.Entities.SettlementItem;
using CashMovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.CommissionSettlement.Commands;

public class PayCommissionSettlementCommand
    : IRequest<PayCommissionSettlementResponse>
{
    public long SettlementId { get; set; }

    public long CashSessionId { get; set; }
}

public class PayCommissionSettlementResponse
{
    public long SettlementId { get; set; }

    public long? CashMovementId { get; set; }

    public decimal TotalAmount { get; set; }

    public int CommissionCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime PaidAt { get; set; }
}

public class PayCommissionSettlementCommandHandler
    : IRequestHandler<
        PayCommissionSettlementCommand,
        PayCommissionSettlementResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public PayCommissionSettlementCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PayCommissionSettlementResponse> Handle(
        PayCommissionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SettlementId <= 0)
            throw new ArgumentException(
                "La liquidación es obligatoria.");

        if (request.CashSessionId <= 0)
            throw new ArgumentException(
                "La sesión de caja es obligatoria.");

        PayCommissionSettlementResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear liquidación

                var settlement = await _unitOfWork
                    .GetCommissionSettlementForUpdateAsync(
                        request.SettlementId, ct);

                if (settlement is null)
                    throw new KeyNotFoundException(
                        "La liquidación no existe.");

                CommissionSettlementRules.ValidatePayment(
                    settlement.Status);

                // 2. Bloquear sesión de caja

                var cashSession = await _unitOfWork
                    .GetCashSessionForUpdateAsync(
                        request.CashSessionId, ct);

                if (cashSession is null)
                    throw new KeyNotFoundException(
                        "La sesión de caja no existe.");

                AppointmentRules.ValidateOpenCashSession(
                    cashSession.Status);

                // 3. Obtener detalles

                var items = (await _unitOfWork
                    .Repository<SettlementItemEntity>()
                    .FindAsync(i =>
                        i.SettlementId == settlement.Id))
                    .ToList();

                // 4. Obtener comisiones rastreadas

                var commissions = new List<CommissionEntity>();

                var commissionRepository = _unitOfWork
                    .Repository<CommissionEntity>();

                foreach (var item in items.OrderBy(i => i.CommissionId))
                {
                    var commission = await commissionRepository
                        .GetByIdAsync(item.CommissionId);

                    if (commission is null)
                        throw new KeyNotFoundException(
                            "Una comisión de la liquidación no existe.");

                    commissions.Add(commission);
                }

                CommissionSettlementRules.ValidateCommissions(
                    settlement.EmployeeId,
                    commissions);

                // 5. Calcular total

                var total = CommissionSettlementRules
                    .CalculateTotal(commissions);

                // 6. Validar efectivo disponible

                var cashMovements = await _unitOfWork
                    .Repository<CashMovementEntity>()
                    .FindAsync(m =>
                        m.CashSessionId == cashSession.Id &&
                        m.Method == "CASH");

                var totalIncome = cashMovements
                    .Where(m => m.Type == "INCOME")
                    .Sum(m => m.Amount);

                var totalExpenses = cashMovements
                    .Where(m => m.Type == "EXPENSE")
                    .Sum(m => m.Amount);

                var availableCash =
                    cashSession.OpeningFloat +
                    totalIncome -
                    totalExpenses;

                if (total > 0 && total > availableCash)
                    throw new ArgumentException(
                        "No existe suficiente efectivo en caja " +
                        "para pagar la liquidación.");

                var now = DateTime.UtcNow;

                CashMovementEntity? movement = null;

                // 7. Registrar egreso de caja

                if (total > 0)
                {
                    movement = new CashMovementEntity
                    {
                        CashSessionId = cashSession.Id,
                        Type = "EXPENSE",
                        Category = "COMMISSION",
                        Method = "CASH",
                        Amount = total,
                        PaymentId = null,
                        Reference = $"LIQUIDACION-{settlement.Id}",
                        Description =
                            $"Pago de comisiones al trabajador " +
                            $"#{settlement.EmployeeId}",
                        Date = now
                    };

                    await _unitOfWork
                        .Repository<CashMovementEntity>()
                        .AddAsync(movement);
                }

                // 8. Actualizar comisiones

                foreach (var commission in commissions)
                {
                    commission.Status = "SETTLED";
                }

                // 9. Actualizar liquidación

                settlement.Status = "PAID";
                settlement.PaidAt = now;

                await _unitOfWork.SaveChangesAsync(ct);

                // 10. Preparar respuesta

                response = new PayCommissionSettlementResponse
                {
                    SettlementId = settlement.Id,
                    CashMovementId = movement?.Id,
                    TotalAmount = total,
                    CommissionCount = commissions.Count,
                    Status = settlement.Status,
                    PaidAt = now
                };
            },
            cancellationToken);

        return response!;
    }
}