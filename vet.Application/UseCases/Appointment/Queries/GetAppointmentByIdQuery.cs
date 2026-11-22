
using MediatR;
using vet.Domain.Ports;
using AppointmentEntity = vet.Domain.Entities.Appointment;
using AppointmentServiceEntity = vet.Domain.Entities.AppointmentService;
using ServiceEntity = vet.Domain.Entities.Service;
using PetEntity = vet.Domain.Entities.Pet;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Appointment.Queries;

public class GetAppointmentByIdQuery : IRequest<AppointmentDetail?>
{
    public long Id { get; set; }
}

public class AppointmentDetail
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ReservedPrice { get; set; }
    public decimal DepositPercent { get; set; }
    public decimal RequiredDeposit { get; set; }
    public DateTime? PaymentExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AppointmentServiceDetail> Services { get; set; } = new();
}

public class AppointmentServiceDetail
{
    public long ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal AgreedPrice { get; set; }
    public int DurationMinutes { get; set; }
}

public class GetAppointmentByIdQueryHandler
    : IRequestHandler<GetAppointmentByIdQuery, AppointmentDetail?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppointmentDetail?> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork
            .Repository<AppointmentEntity>()
            .GetByIdAsync(request.Id);

        if (appointment is null)
            return null;

        var pet = await _unitOfWork
            .Repository<PetEntity>()
            .GetByIdAsync(appointment.PetId);

        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(appointment.EmployeeId);

        var appointmentServices = (await _unitOfWork
            .Repository<AppointmentServiceEntity>()
            .FindAsync(x => x.AppointmentId == request.Id))
            .ToList();

        var serviceIds = appointmentServices
            .Select(x => x.ServiceId)
            .ToList();

        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(x => serviceIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        return new AppointmentDetail
        {
            Id = appointment.Id,
            PetId = appointment.PetId,
            PetName = pet?.Name ?? "Sin mascota",
            EmployeeId = appointment.EmployeeId,
            EmployeeName = employee is null
                ? "Sin trabajador"
                : $"{employee.FirstNames} {employee.LastNames}",
            StartAt = appointment.StartAt,
            EndAt = appointment.EndAt,
            Status = appointment.Status,
            ReservedPrice = appointment.ReservedPrice,
            DepositPercent = appointment.DepositPercent,
            RequiredDeposit = appointment.RequiredDeposit,
            PaymentExpiresAt = appointment.PaymentExpiresAt,
            CreatedAt = appointment.CreatedAt,

            Services = appointmentServices.Select(item =>
            {
                services.TryGetValue(item.ServiceId, out var service);

                return new AppointmentServiceDetail
                {
                    ServiceId = item.ServiceId,
                    ServiceName = service?.Name ?? "Servicio no encontrado",
                    AgreedPrice = item.AgreedPrice,
                    DurationMinutes = service?.DurationMinutes ?? 0
                };
            }).ToList()
        };
    }
}
