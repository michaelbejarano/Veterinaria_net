using MediatR;
using vet.Domain.Ports;

using SaleEntity = vet.Domain.Entities.Sale;
using SaleItemEntity = vet.Domain.Entities.SaleItem;
using AppointmentEntity = vet.Domain.Entities.Appointment;
using PetEntity = vet.Domain.Entities.Pet;
using EmployeeEntity = vet.Domain.Entities.Employee;
using OwnerEntity = vet.Domain.Entities.Owner;
using CommissionEntity = vet.Domain.Entities.Commission;
using CashMovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.Report.Queries;

public class GetDashboardQuery : IRequest<DashboardDto>
{
    public DateOnly? Date { get; set; }
}

public class DashboardDto
{
    public DateOnly Date { get; set; }

    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int InProgressAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }
    public int ExpiredAppointments { get; set; }

    public int ActivePets { get; set; }
    public int TotalOwners { get; set; }
    public int ActiveEmployees { get; set; }

    public int TotalSales { get; set; }
    public decimal SalesAmount { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetBalance => TotalIncome - TotalExpenses;

    public decimal PendingCommissions { get; set; }

    public List<DailyIncomeDto> DailyIncome { get; set; } = new();
}

public class DailyIncomeDto
{
    public DateOnly Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance => Income - Expenses;
}

public class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardDto> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var date = request.Date ?? ReportHelper.Today();

        if (date.DayNumber < 6)
            throw new ArgumentException("Fecha inválida.");

        var (start, end) = ReportHelper.GetRange(date, date);

        var (weekStart, weekEnd) =
            ReportHelper.GetRange(date.AddDays(-6), date);

        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x => x.StartAt >= start && x.StartAt < end);

        var sales = await _unitOfWork
            .Repository<SaleEntity>()
            .FindAsync(x =>
                x.Status == "CONFIRMED" &&
                x.Date >= start &&
                x.Date < end);

        var saleIds = sales.Select(x => x.Id).ToList();

        var items = await _unitOfWork
            .Repository<SaleItemEntity>()
            .FindAsync(x => saleIds.Contains(x.SaleId));

        var movements = await _unitOfWork
            .Repository<CashMovementEntity>()
            .FindAsync(x =>
                x.Date >= weekStart &&
                x.Date < weekEnd);

        var todayMovements = movements
            .Where(x => x.Date >= start && x.Date < end)
            .ToList();

        var pets = await _unitOfWork
            .Repository<PetEntity>()
            .FindAsync(x => x.IsActive);

        var employees = await _unitOfWork
            .Repository<EmployeeEntity>()
            .FindAsync(x => x.IsActive);

        var owners = await _unitOfWork
            .Repository<OwnerEntity>()
            .GetAllAsync();

        var commissions = await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(x => x.Status == "PENDING");

        var dailyIncome = new List<DailyIncomeDto>();

        for (int i = 6; i >= 0; i--)
        {
            var currentDate = date.AddDays(-i);

            var dailyMovements = movements
                .Where(x => ReportHelper.LocalDate(x.Date) == currentDate)
                .ToList();

            dailyIncome.Add(new DailyIncomeDto
            {
                Date = currentDate,

                Income = dailyMovements
                    .Where(x => x.Type == "INCOME")
                    .Sum(x => x.Amount),

                Expenses = dailyMovements
                    .Where(x => x.Type == "EXPENSE")
                    .Sum(x => x.Amount)
            });
        }

        return new DashboardDto
        {
            Date = date,

            TotalAppointments = appointments.Count(),

            PendingAppointments = appointments.Count(
                x => x.Status == "PENDING_PAYMENT"),

            ConfirmedAppointments = appointments.Count(
                x => x.Status == "CONFIRMED"),

            InProgressAppointments = appointments.Count(
                x => x.Status == "IN_PROGRESS"),

            CompletedAppointments = appointments.Count(
                x => x.Status == "COMPLETED"),

            CancelledAppointments = appointments.Count(
                x => x.Status == "CANCELLED"),

            NoShowAppointments = appointments.Count(
                x => x.Status == "NO_SHOW"),

            ExpiredAppointments = appointments.Count(
                x => x.Status == "EXPIRED"),

            ActivePets = pets.Count(),
            TotalOwners = owners.Count(),
            ActiveEmployees = employees.Count(),

            TotalSales = sales.Count(),
            SalesAmount = items.Sum(ReportHelper.ItemTotal),

            TotalIncome = todayMovements
                .Where(x => x.Type == "INCOME")
                .Sum(x => x.Amount),

            TotalExpenses = todayMovements
                .Where(x => x.Type == "EXPENSE")
                .Sum(x => x.Amount),

            PendingCommissions = commissions.Sum(x => x.Amount),

            DailyIncome = dailyIncome
        };
    }
}