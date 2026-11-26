using MediatR;
using vet.Domain.Ports;
using CommissionEntity = vet.Domain.Entities.Commission;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Report.Queries;

public class GetCommissionsReportQuery
    : IRequest<CommissionsReportDto>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? EmployeeId { get; set; }
}

public class CommissionsReportDto
{
    public int TotalCommissions { get; set; }
    public decimal TotalGenerated { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalSettled { get; set; }
    public decimal TotalVoided { get; set; }

    public List<EmployeeCommissionReportDto> Employees
        { get; set; } = new();
}

public class EmployeeCommissionReportDto
{
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int CommissionCount { get; set; }
    public decimal Generated { get; set; }
    public decimal Pending { get; set; }
    public decimal Settled { get; set; }
    public decimal Voided { get; set; }
}

public class GetCommissionsReportQueryHandler
    : IRequestHandler<GetCommissionsReportQuery, CommissionsReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCommissionsReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CommissionsReportDto> Handle(
        GetCommissionsReportQuery request,
        CancellationToken cancellationToken)
    {
        var (start, end) = ReportHelper.GetRange(
            request.FromDate, request.ToDate);

        var commissions = await _unitOfWork
            .Repository<CommissionEntity>()
            .FindAsync(x =>
                x.Date >= start &&
                x.Date < end &&
                (!request.EmployeeId.HasValue ||
                 x.EmployeeId == request.EmployeeId.Value));

        var employeeIds = commissions
            .Select(x => x.EmployeeId).Distinct().ToList();

        var employees = (await _unitOfWork
            .Repository<EmployeeEntity>()
            .FindAsync(x => employeeIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        return new CommissionsReportDto
        {
            TotalCommissions = commissions.Count(),

            TotalGenerated = commissions
                .Where(x => x.Status != "VOIDED")
                .Sum(x => x.Amount),

            TotalPending = commissions
                .Where(x => x.Status == "PENDING")
                .Sum(x => x.Amount),

            TotalSettled = commissions
                .Where(x => x.Status == "SETTLED")
                .Sum(x => x.Amount),

            TotalVoided = commissions
                .Where(x => x.Status == "VOIDED")
                .Sum(x => x.Amount),

            Employees = commissions
                .GroupBy(x => x.EmployeeId)
                .Select(g => new EmployeeCommissionReportDto
                {
                    EmployeeId = g.Key,
                    EmployeeName =
                        employees[g.Key].FirstNames + " " +
                        employees[g.Key].LastNames,

                    CommissionCount = g.Count(),

                    Generated = g.Where(x => x.Status != "VOIDED")
                        .Sum(x => x.Amount),

                    Pending = g.Where(x => x.Status == "PENDING")
                        .Sum(x => x.Amount),

                    Settled = g.Where(x => x.Status == "SETTLED")
                        .Sum(x => x.Amount),

                    Voided = g.Where(x => x.Status == "VOIDED")
                        .Sum(x => x.Amount)

                }).OrderByDescending(x => x.Generated).ToList()
        };
    }
}