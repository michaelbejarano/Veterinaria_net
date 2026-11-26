using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using CashMovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.CashSession.Commands;

public class CloseCashSessionCommand
    : IRequest<CloseCashSessionResult>
{
    public long CashSessionId { get; set; }
    public long ClosedByUserId { get; set; }
    public decimal CountedCash { get; set; }
}

public class CloseCashSessionResult
{
    public long CashSessionId { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CloseCashSessionCommandHandler
    : IRequestHandler<
        CloseCashSessionCommand,
        CloseCashSessionResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CloseCashSessionCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CloseCashSessionResult> Handle(
        CloseCashSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CashSessionId <= 0)
            throw new ArgumentException(
                "La sesión de caja no es válida.");

        if (request.ClosedByUserId <= 0)
            throw new ArgumentException(
                "El usuario autenticado no es válido.");

        CloseCashSessionResult? result = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear sesión de caja

                var cashSession = await _unitOfWork
                    .GetCashSessionForUpdateAsync(
                        request.CashSessionId, ct);

                if (cashSession is null)
                    throw new KeyNotFoundException(
                        "La sesión de caja no existe.");

                CashSessionRules.ValidateClosing(
                    cashSession.Status,
                    request.CountedCash);

                // 2. Obtener movimientos

                var movements = await _unitOfWork
                    .Repository<CashMovementEntity>()
                    .FindAsync(m =>
                        m.CashSessionId == cashSession.Id);

                // 3. Calcular efectivo esperado

                var expectedCash =
                    CashSessionRules.CalculateExpectedCash(
                        cashSession.OpeningFloat,
                        movements);

                // 4. Cerrar sesión

                cashSession.Status = "CLOSED";
                cashSession.ClosedAt = DateTime.UtcNow;
                cashSession.ClosedByUserId =
                    request.ClosedByUserId;

                cashSession.CountedCash =
                    request.CountedCash;

                await _unitOfWork.SaveChangesAsync(ct);

                // 5. Construir resultado

                result = new CloseCashSessionResult
                {
                    CashSessionId = cashSession.Id,
                    ExpectedCash = expectedCash,
                    CountedCash = request.CountedCash,

                    Difference =
                        request.CountedCash - expectedCash,

                    Status = cashSession.Status
                };
            },
            cancellationToken);

        return result!;
    }
}