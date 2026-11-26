using MediatR;
using vet.Domain.Ports;
using AppointmentEntity = vet.Domain.Entities.Appointment;
using EmployeeEntity = vet.Domain.Entities.Employee;
using PetEntity = vet.Domain.Entities.Pet;

namespace vet.Application.UseCases.Report.Queries;

public class GetAppointmentsReportQuery
    : IRequest<AppointmentsReportDto>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? EmployeeId { get; set; }
    public string? Status { get; set; }
}

public class AppointmentsReportDto
{
    public int Total { get; set; }
    public List<AppointmentStatusReportDto> ByStatus { get; set; } = new();
    public List<AppointmentReportItemDto> Appointments { get; set; } = new();
}

public class AppointmentStatusReportDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AppointmentReportItemDto
{
    public long Id { get; set; }
    public string PetName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ReservedPrice { get; set; }
    public decimal RequiredDeposit { get; set; }
}

public class GetAppointmentsReportQueryHandler
    : IRequestHandler<GetAppointmentsReportQuery, AppointmentsReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentsReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppointmentsReportDto> Handle(
        GetAppointmentsReportQuery request,
        CancellationToken cancellationToken)
    {
        var (start, end) = ReportHelper.GetRange(
            request.FromDate, request.ToDate);

        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToUpperInvariant();

        if (status is not null &&
            status is not (
                "PENDING_PAYMENT" or "CONFIRMED" or
                "IN_PROGRESS" or "COMPLETED" or
                "CANCELLED" or "NO_SHOW" or "EXPIRED"))
        {
            throw new ArgumentException("Estado de cita inválido.");
        }

        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.StartAt >= start &&
                x.StartAt < end &&
                (!request.EmployeeId.HasValue ||
                 x.EmployeeId == request.EmployeeId.Value) &&
                (status == null || x.Status == status));

        var employeeIds = appointments
            .Select(x => x.EmployeeId).Distinct().ToList();

        var petIds = appointments
            .Select(x => x.PetId).Distinct().ToList();

        var employees = (await _unitOfWork
            .Repository<EmployeeEntity>()
            .FindAsync(x => employeeIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var pets = (await _unitOfWork
            .Repository<PetEntity>()
            .FindAsync(x => petIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        return new AppointmentsReportDto
        {
            Total = appointments.Count(),

            ByStatus = appointments
                .GroupBy(x => x.Status)
                .Select(g => new AppointmentStatusReportDto
                {
                    Status = g.Key,
                    Count = g.Count()
                }).ToList(),

            Appointments = appointments
                .OrderBy(x => x.StartAt)
                .Select(x => new AppointmentReportItemDto
                {
                    Id = x.Id,
                    PetName = pets[x.PetId].Name,
                    EmployeeName =
                        employees[x.EmployeeId].FirstNames + " " +
                        employees[x.EmployeeId].LastNames,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    Status = x.Status,
                    ReservedPrice = x.ReservedPrice,
                    RequiredDeposit = x.RequiredDeposit
                }).ToList()
        };
    }
}