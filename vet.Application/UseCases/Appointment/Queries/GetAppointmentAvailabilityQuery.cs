
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using AppointmentEntity = vet.Domain.Entities.Appointment;
using EmployeeEntity = vet.Domain.Entities.Employee;
using ServiceEntity = vet.Domain.Entities.Service;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;
using EmployeeScheduleEntity = vet.Domain.Entities.EmployeeSchedule;
using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.Appointment.Queries;

public class GetAppointmentAvailabilityQuery
    : IRequest<List<AvailableAppointmentSlot>>
{
    public long EmployeeId { get; set; }
    public long ServiceId { get; set; }
    public DateOnly Date { get; set; }
}

public class AvailableAppointmentSlot
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}

public class GetAppointmentAvailabilityQueryHandler
    : IRequestHandler<GetAppointmentAvailabilityQuery,
        List<AvailableAppointmentSlot>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentAvailabilityQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AvailableAppointmentSlot>> Handle(
        GetAppointmentAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null || !employee.IsActive)
            throw new ArgumentException(
                "El trabajador no existe o está inactivo.");

        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.ServiceId);

        if (service is null || !service.IsActive)
            throw new ArgumentException(
                "El servicio no existe o está inactivo.");

        if (service.DurationMinutes <= 0)
            throw new ArgumentException(
                "La duración del servicio debe ser mayor que cero.");

        var assignments = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.ServiceId == request.ServiceId &&
                x.IsEnabled);

        if (!assignments.Any())
            throw new ArgumentException(
                "El trabajador no tiene asignado este servicio.");

        var peruZone = TimeZoneInfo.FindSystemTimeZoneById(
            "America/Lima");

        var localDay = request.Date.ToDateTime(
            TimeOnly.MinValue);

        var nextLocalDay = localDay.AddDays(1);

        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            localDay, peruZone);

        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(
            nextLocalDay, peruZone);

        short weekday = (short)(
            localDay.DayOfWeek == DayOfWeek.Sunday
                ? 7
                : (int)localDay.DayOfWeek);

        var schedules = (await _unitOfWork
            .Repository<EmployeeScheduleEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.Weekday == weekday))
            .OrderBy(x => x.StartAt)
            .ToList();

        var appointments = (await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.Status != "CANCELLED" &&
                x.Status != "EXPIRED" &&
                x.StartAt < dayEndUtc &&
                x.EndAt > dayStartUtc))
            .ToList();

        var blocks = (await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.StartAt < dayEndUtc &&
                x.EndAt > dayStartUtc))
            .ToList();

        var result = new List<AvailableAppointmentSlot>();
        var nowUtc = DateTime.UtcNow;

        foreach (var schedule in schedules)
        {
            var slotStart = request.Date.ToDateTime(
                schedule.StartAt);

            var shiftEnd = request.Date.ToDateTime(
                schedule.EndAt);

            while (slotStart.AddMinutes(
                       service.DurationMinutes) <= shiftEnd)
            {
                var slotEnd = slotStart.AddMinutes(
                    service.DurationMinutes);

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                    slotStart, peruZone);

                var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                    slotEnd, peruZone);

                var appointmentConflict = appointments.Any(a =>
                    AppointmentRules.Overlaps(
                        startUtc, endUtc,
                        a.StartAt, a.EndAt));

                var blockConflict = blocks.Any(b =>
                    AppointmentRules.Overlaps(
                        startUtc, endUtc,
                        b.StartAt, b.EndAt));

                if (startUtc > nowUtc &&
                    !appointmentConflict &&
                    !blockConflict)
                {
                    result.Add(new AvailableAppointmentSlot
                    {
                        StartAt = new DateTimeOffset(
                            slotStart,
                            peruZone.GetUtcOffset(slotStart)),

                        EndAt = new DateTimeOffset(
                            slotEnd,
                            peruZone.GetUtcOffset(slotEnd))
                    });
                }

                slotStart = slotEnd;
            }
        }

        return result;
    }
}
