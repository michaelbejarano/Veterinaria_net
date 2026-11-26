using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Commission;

using CommissionEntity = vet.Domain.Entities.Commission;
using SettlementEntity = vet.Domain.Entities.CommissionSettlement;
using SettlementItemEntity = vet.Domain.Entities.SettlementItem;

namespace vet.Application.UseCases.CommissionSettlement.Commands;

public class CreateCommissionSettlementCommand : IRequest<long>
{
    public long EmployeeId { get; set; }

    public DateOnly Date { get; set; }
}

public class CreateCommissionSettlementCommandHandler
    : IRequestHandler<CreateCommissionSettlementCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCommissionSettlementCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateCommissionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        long settlementId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var timeZone = TimeZoneInfo
                    .FindSystemTimeZoneById("America/Lima");

                var today = DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.UtcNow, timeZone));

                // 1. Validar datos

                CommissionSettlementRules.ValidateCreation(
                    request.EmployeeId,
                    request.Date,
                    today);

                // 2. Bloquear trabajador

                var employee = await _unitOfWork
                    .GetEmployeeForUpdateAsync(
                        request.EmployeeId, ct);

                if (employee is null)
                    throw new KeyNotFoundException(
                        "El trabajador no existe.");

                // 3. Verificar liquidación existente

                var existingSettlements = await _unitOfWork
                    .Repository<SettlementEntity>()
                    .FindAsync(s =>
                        s.EmployeeId == request.EmployeeId &&
                        s.Date == request.Date);

                if (existingSettlements.Any())
                    throw new ArgumentException(
                        "Ya existe una liquidación para este trabajador y fecha.");

                // 4. Calcular límite de fecha

                var localEnd = request.Date
                    .AddDays(1)
                    .ToDateTime(TimeOnly.MinValue);

                var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                    localEnd, timeZone);

                // 5. Obtener comisiones pendientes

                var pendingCommissions = (await _unitOfWork
                    .Repository<CommissionEntity>()
                    .FindAsync(c =>
                        c.EmployeeId == request.EmployeeId &&
                        c.Status == "PENDING" &&
                        c.Date < endUtc))
                    .ToList();

                var commissionIds = pendingCommissions
                    .Select(c => c.Id)
                    .ToList();

                // 6. Excluir comisiones ya asociadas

                var existingItems = await _unitOfWork
                    .Repository<SettlementItemEntity>()
                    .FindAsync(i =>
                        commissionIds.Contains(i.CommissionId));

                var assignedIds = existingItems
                    .Select(i => i.CommissionId)
                    .ToHashSet();

                var commissions = pendingCommissions
                    .Where(c => !assignedIds.Contains(c.Id))
                    .ToList();

                CommissionSettlementRules.ValidateCommissions(
                    request.EmployeeId,
                    commissions);

                // 7. Crear liquidación

                var settlement = new SettlementEntity
                {
                    EmployeeId = request.EmployeeId,
                    Date = request.Date,
                    Status = "PENDING",
                    PaidAt = null
                };

                await _unitOfWork
                    .Repository<SettlementEntity>()
                    .AddAsync(settlement);

                await _unitOfWork.SaveChangesAsync(ct);

                // 8. Registrar detalles

                foreach (var commission in commissions)
                {
                    var item = new SettlementItemEntity
                    {
                        SettlementId = settlement.Id,
                        CommissionId = commission.Id
                    };

                    await _unitOfWork
                        .Repository<SettlementItemEntity>()
                        .AddAsync(item);
                }

                await _unitOfWork.SaveChangesAsync(ct);

                settlementId = settlement.Id;
            },
            cancellationToken);

        return settlementId;
    }
}