using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using SessionEntity = vet.Domain.Entities.CashSession;
using RegisterEntity = vet.Domain.Entities.CashRegister;
using MovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.CashSession.Queries;

public record GetCashSessionSummaryQuery(long CashSessionId)
    : IRequest<CashSessionSummaryDto>;

public class CashSessionSummaryDto
{
    public long CashSessionId { get; set; }
    public long CashRegisterId { get; set; }
    public string CashRegisterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public decimal OpeningFloat { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetMovement { get; set; }

    public decimal CashIncome { get; set; }
    public decimal CashExpense { get; set; }
    public decimal ExpectedCash { get; set; }

    public decimal? CountedCash { get; set; }
    public decimal? Difference { get; set; }

    public int MovementCount { get; set; }

    public List<CashMethodSummaryDto> ByMethod { get; set; }
        = new();

    public List<CashCategorySummaryDto> ByCategory { get; set; }
        = new();
}

public class CashMethodSummaryDto
{
    public string Method { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}

public class CashCategorySummaryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public int MovementCount { get; set; }
}

public class GetCashSessionSummaryQueryHandler
    : IRequestHandler<
        GetCashSessionSummaryQuery,
        CashSessionSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashSessionSummaryQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashSessionSummaryDto> Handle(
        GetCashSessionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _unitOfWork
            .Repository<SessionEntity>()
            .GetByIdAsync(request.CashSessionId);

        if (session is null)
            throw new KeyNotFoundException(
                "La sesión de caja no existe.");

        var register = await _unitOfWork
            .Repository<RegisterEntity>()
            .GetByIdAsync(session.CashRegisterId);

        if (register is null)
            throw new KeyNotFoundException("La caja no existe.");

        var movements = await _unitOfWork
            .Repository<MovementEntity>()
            .FindAsync(x =>
                x.CashSessionId == session.Id);

        // 1. Totales generales

        var totalIncome = movements
            .Where(x => x.Type == "INCOME")
            .Sum(x => x.Amount);

        var totalExpense = movements
            .Where(x => x.Type == "EXPENSE")
            .Sum(x => x.Amount);

        // 2. Totales en efectivo

        var cashIncome = movements
            .Where(x =>
                x.Method == "CASH" &&
                x.Type == "INCOME")
            .Sum(x => x.Amount);

        var cashExpense = movements
            .Where(x =>
                x.Method == "CASH" &&
                x.Type == "EXPENSE")
            .Sum(x => x.Amount);

        var expectedCash =
            CashSessionRules.CalculateExpectedCash(
                session.OpeningFloat,
                movements);

        // 3. Resumen por método

        var methods = new[]
        {
            "CASH",
            "YAPE",
            "PLIN",
            "CARD",
            "BANK_TRANSFER"
        };

        var byMethod = methods
            .Select(method =>
            {
                var income = movements
                    .Where(x =>
                        x.Method == method &&
                        x.Type == "INCOME")
                    .Sum(x => x.Amount);

                var expense = movements
                    .Where(x =>
                        x.Method == method &&
                        x.Type == "EXPENSE")
                    .Sum(x => x.Amount);

                return new CashMethodSummaryDto
                {
                    Method = method,
                    Income = income,
                    Expense = expense,
                    Net = income - expense
                };
            })
            .ToList();

        // 4. Resumen por categoría

        var byCategory = movements
            .GroupBy(x => x.Category)
            .Select(group => new CashCategorySummaryDto
            {
                Category = group.Key,

                Income = group
                    .Where(x => x.Type == "INCOME")
                    .Sum(x => x.Amount),

                Expense = group
                    .Where(x => x.Type == "EXPENSE")
                    .Sum(x => x.Amount),

                MovementCount = group.Count()
            })
            .OrderBy(x => x.Category)
            .ToList();

        // 5. Construir respuesta

        return new CashSessionSummaryDto
        {
            CashSessionId = session.Id,
            CashRegisterId = register.Id,
            CashRegisterName = register.Name,
            Status = session.Status,

            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,

            OpeningFloat = session.OpeningFloat,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetMovement = totalIncome - totalExpense,

            CashIncome = cashIncome,
            CashExpense = cashExpense,
            ExpectedCash = expectedCash,

            CountedCash = session.CountedCash,

            Difference = session.CountedCash.HasValue
                ? session.CountedCash.Value - expectedCash
                : null,

            MovementCount = movements.Count(),

            ByMethod = byMethod,
            ByCategory = byCategory
        };
    }
}