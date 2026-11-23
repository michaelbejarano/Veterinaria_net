using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using CashSessionEntity = vet.Domain.Entities.CashSession;
using CashMovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.CashSession.Commands;

public class CloseCashSessionCommand : IRequest<CloseCashSessionResult>
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
    : IRequestHandler<CloseCashSessionCommand, CloseCashSessionResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CloseCashSessionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CloseCashSessionResult> Handle(
        CloseCashSessionCommand request,
        CancellationToken cancellationToken)
    {
        CloseCashSessionResult? result = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var cashSession = await _unitOfWork
                .Repository<CashSessionEntity>()
                .GetByIdAsync(request.CashSessionId);

            if (cashSession is null)
                throw new KeyNotFoundException(
                    "La sesión de caja no existe.");

            CashSessionRules.ValidateClosing(
                cashSession.Status,
                request.CountedCash);

            var movements = await _unitOfWork
                .Repository<CashMovementEntity>()
                .FindAsync(x => x.CashSessionId == cashSession.Id);

            var expectedCash = CashSessionRules.CalculateExpectedCash(
                cashSession.OpeningFloat,
                movements);

            cashSession.Status = "CLOSED";
            cashSession.ClosedAt = DateTime.UtcNow;
            cashSession.ClosedByUserId = request.ClosedByUserId;
            cashSession.CountedCash = request.CountedCash;

            result = new CloseCashSessionResult
            {
                CashSessionId = cashSession.Id,
                ExpectedCash = expectedCash,
                CountedCash = request.CountedCash,
                Difference = request.CountedCash - expectedCash,
                Status = cashSession.Status
            };

        }, cancellationToken);

        return result!;
    }
}