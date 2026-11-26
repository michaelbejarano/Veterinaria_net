using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using CashMovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.CashSession.Commands;

public class RegisterCashMovementCommand : IRequest<long>
{
    public long CashSessionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
}

public class RegisterCashMovementCommandHandler
    : IRequestHandler<RegisterCashMovementCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCashMovementCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        RegisterCashMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CashSessionId <= 0)
            throw new ArgumentException(
                "La sesión de caja no es válida.");

        var type = request.Type?
            .Trim().ToUpperInvariant() ?? string.Empty;

        var category = request.Category?
            .Trim().ToUpperInvariant() ?? string.Empty;

        var reference = string.IsNullOrWhiteSpace(
            request.Reference)
            ? null
            : request.Reference.Trim();

        if (reference is not null &&
            reference.Length > 120)
            throw new ArgumentException(
                "La referencia admite máximo 120 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException(
                "Debe indicar el motivo del movimiento.");

        CashSessionRules.ValidateMoney(request.Amount);

        long movementId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear sesión

                var cashSession = await _unitOfWork
                    .GetCashSessionForUpdateAsync(
                        request.CashSessionId, ct);

                if (cashSession is null)
                    throw new KeyNotFoundException(
                        "La sesión de caja no existe.");

                // 2. Validar movimiento

                CashMovementRules.ValidateManualMovement(
                    cashSession.Status,
                    type,
                    category,
                    "CASH",
                    request.Amount);

                var repository = _unitOfWork
                    .Repository<CashMovementEntity>();

                // 3. Validar saldo para egresos

                if (type == "EXPENSE")
                {
                    var movements = await repository
                        .FindAsync(m =>
                            m.CashSessionId == cashSession.Id);

                    var availableCash =
                        CashSessionRules.CalculateExpectedCash(
                            cashSession.OpeningFloat,
                            movements);

                    CashSessionRules.ValidateAvailableCash(
                        request.Amount,
                        availableCash);
                }

                // 4. Registrar movimiento

                var movement = new CashMovementEntity
                {
                    CashSessionId = cashSession.Id,
                    Type = type,
                    Category = category,
                    Method = "CASH",
                    Amount = request.Amount,
                    Description = request.Description.Trim(),
                    Reference = reference,
                    PaymentId = null,
                    Date = DateTime.UtcNow
                };

                await repository.AddAsync(movement);

                await _unitOfWork.SaveChangesAsync(ct);

                movementId = movement.Id;
            },
            cancellationToken);

        return movementId;
    }
}