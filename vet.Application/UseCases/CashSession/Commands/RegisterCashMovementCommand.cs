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
        long movementId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var cashSession =
                    await _unitOfWork.GetCashSessionForUpdateAsync(
                        request.CashSessionId, ct);

                if (cashSession is null)
                    throw new KeyNotFoundException(
                        "La sesión de caja no existe.");

                CashMovementRules.ValidateManualMovement(
                    cashSession.Status,
                    request.Type,
                    request.Category,
                    "CASH",
                    request.Amount);

                if (string.IsNullOrWhiteSpace(request.Description))
                    throw new ArgumentException(
                        "Debe indicar el motivo del movimiento.");

                var movement = new CashMovementEntity
                {
                    CashSessionId = cashSession.Id,
                    Type = request.Type,
                    Category = request.Category,
                    Method = "CASH",
                    Amount = request.Amount,
                    Description = request.Description.Trim(),
                    Reference = request.Reference,
                    Date = DateTime.UtcNow
                };

                await _unitOfWork
                    .Repository<CashMovementEntity>()
                    .AddAsync(movement);

                await _unitOfWork.SaveChangesAsync(ct);

                movementId = movement.Id;
            },
            cancellationToken);

        return movementId;
    }
}