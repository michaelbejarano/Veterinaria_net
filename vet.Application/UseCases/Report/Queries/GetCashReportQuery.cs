using MediatR;
using vet.Domain.Ports;
using MovementEntity = vet.Domain.Entities.CashMovement;
using SessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.Report.Queries;

public class GetCashReportQuery : IRequest<CashReportDto>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? CashRegisterId { get; set; }
}

public class CashReportDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Balance => TotalIncome - TotalExpenses;
    public int TotalMovements { get; set; }

    public List<CashGroupReportDto> ByMethod { get; set; } = new();
    public List<CashGroupReportDto> ByCategory { get; set; } = new();
    public List<DailyIncomeDto> ByDate { get; set; } = new();
}

public class CashGroupReportDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance => Income - Expenses;
    public int Count { get; set; }
}

public class GetCashReportQueryHandler
    : IRequestHandler<GetCashReportQuery, CashReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashReportDto> Handle(
        GetCashReportQuery request,
        CancellationToken cancellationToken)
    {
        var (start, end) = ReportHelper.GetRange(
            request.FromDate, request.ToDate);

        var sessions = await _unitOfWork
            .Repository<SessionEntity>()
            .FindAsync(x =>
                !request.CashRegisterId.HasValue ||
                x.CashRegisterId == request.CashRegisterId.Value);

        var sessionIds = sessions.Select(x => x.Id).ToList();

        var movements = await _unitOfWork
            .Repository<MovementEntity>()
            .FindAsync(x =>
                sessionIds.Contains(x.CashSessionId) &&
                x.Date >= start &&
                x.Date < end);

        return new CashReportDto
        {
            TotalIncome = movements
                .Where(x => x.Type == "INCOME")
                .Sum(x => x.Amount),

            TotalExpenses = movements
                .Where(x => x.Type == "EXPENSE")
                .Sum(x => x.Amount),

            TotalMovements = movements.Count(),

            ByMethod = movements
                .GroupBy(x => x.Method)
                .Select(g => new CashGroupReportDto
                {
                    Name = g.Key,
                    Income = g.Where(x => x.Type == "INCOME")
                        .Sum(x => x.Amount),
                    Expenses = g.Where(x => x.Type == "EXPENSE")
                        .Sum(x => x.Amount),
                    Count = g.Count()
                }).ToList(),

            ByCategory = movements
                .GroupBy(x => x.Category)
                .Select(g => new CashGroupReportDto
                {
                    Name = g.Key,
                    Income = g.Where(x => x.Type == "INCOME")
                        .Sum(x => x.Amount),
                    Expenses = g.Where(x => x.Type == "EXPENSE")
                        .Sum(x => x.Amount),
                    Count = g.Count()
                }).ToList(),

            ByDate = movements
                .GroupBy(x => ReportHelper.LocalDate(x.Date))
                .OrderBy(g => g.Key)
                .Select(g => new DailyIncomeDto
                {
                    Date = g.Key,
                    Income = g.Where(x => x.Type == "INCOME")
                        .Sum(x => x.Amount),
                    Expenses = g.Where(x => x.Type == "EXPENSE")
                        .Sum(x => x.Amount)
                }).ToList()
        };
    }
}