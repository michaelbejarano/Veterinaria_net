using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Application.UseCases.Appointment.Queries;

using AppointmentEntity =
    vet.Domain.Entities.Appointment;

using EmployeeEntity =
    vet.Domain.Entities.Employee;

using EmployeeScheduleEntity =
    vet.Domain.Entities.EmployeeSchedule;

using ScheduleBlockEntity =
    vet.Domain.Entities.ScheduleBlock;

using AppointmentRescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

namespace vet.Application.UseCases.AppointmentReschedule.Queries;

public class GetRescheduleAvailabilityQuery
    : IRequest<List<AvailableAppointmentSlot>>
{
    public long AppointmentId { get; set; }

    public DateOnly Date { get; set; }
}

public class GetRescheduleAvailabilityQueryHandler
    : IRequestHandler<
        GetRescheduleAvailabilityQuery,
        List<AvailableAppointmentSlot>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRescheduleAvailabilityQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AvailableAppointmentSlot>> Handle(
        GetRescheduleAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Validar identificador

        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        // 2. Obtener cita original

        var appointment = await _unitOfWork
            .Repository<AppointmentEntity>()
            .GetByIdAsync(request.AppointmentId);

        if (appointment is null)
            throw new ArgumentException(
                "La cita no existe.");

        // 3. Validar estado y hora de inicio

        if (appointment.Status != "CONFIRMED")
            throw new ArgumentException(
                "Solo se pueden reprogramar citas confirmadas.");

        if (appointment.StartAt <= DateTime.UtcNow)
            throw new ArgumentException(
                "La hora de inicio de la cita ya llegó.");

        // 4. Verificar solicitudes pendientes

        var pendingReschedules = await _unitOfWork
            .Repository<AppointmentRescheduleEntity>()
            .FindAsync(x =>
                x.AppointmentId == appointment.Id &&
                x.Status == "PENDING_PAYMENT");

        if (pendingReschedules.Any())
            throw new ArgumentException(
                "La cita ya tiene una reprogramación pendiente.");

        // 5. Validar trabajador original

        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(appointment.EmployeeId);

        if (employee is null || !employee.IsActive)
            throw new ArgumentException(
                "El trabajador no existe o está inactivo.");

        // 6. Conservar duración original

        var requiredMinutes = (int)(
            appointment.EndAt -
            appointment.StartAt).TotalMinutes;

        if (requiredMinutes <= 0)
            throw new ArgumentException(
                "La duración original de la cita es inválida.");

        // 7. Configurar zona horaria de Perú

        var peruZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "America/Lima");

        var nowUtc = DateTime.UtcNow;

        var localNow =
            TimeZoneInfo.ConvertTimeFromUtc(
                nowUtc, peruZone);

        if (request.Date < DateOnly.FromDateTime(localNow))
            return new List<AvailableAppointmentSlot>();

        var localDay = request.Date.ToDateTime(
            TimeOnly.MinValue);

        var nextLocalDay = localDay.AddDays(1);

        var dayStartUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localDay, peruZone);

        var dayEndUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                nextLocalDay, peruZone);

        short weekday = (short)(
            localDay.DayOfWeek == DayOfWeek.Sunday
                ? 7
                : (int)localDay.DayOfWeek);

        // 8. Obtener horarios laborales

        var schedules = (await _unitOfWork
            .Repository<EmployeeScheduleEntity>()
            .FindAsync(x =>
                x.EmployeeId == appointment.EmployeeId &&
                x.Weekday == weekday))
            .OrderBy(x => x.StartAt)
            .ToList();

        // 9. Obtener citas del trabajador
        // Excluir la cita que estamos reprogramando

        var employeeAppointments = (await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.Id != appointment.Id &&
                x.EmployeeId == appointment.EmployeeId &&
                x.Status != "CANCELLED" &&
                x.Status != "EXPIRED" &&
                x.StartAt < dayEndUtc &&
                x.EndAt > dayStartUtc))
            .ToList();

        // 10. Obtener citas de la mascota
        // Excluir la cita original

        var petAppointments = (await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.Id != appointment.Id &&
                x.PetId == appointment.PetId &&
                x.Status != "CANCELLED" &&
                x.Status != "EXPIRED" &&
                x.StartAt < dayEndUtc &&
                x.EndAt > dayStartUtc))
            .ToList();

        // 11. Obtener bloqueos de agenda

        var blocks = (await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .FindAsync(x =>
                x.EmployeeId == appointment.EmployeeId &&
                x.StartAt < dayEndUtc &&
                x.EndAt > dayStartUtc))
            .ToList();

        // 12. Generar horarios disponibles

        var result = new List<AvailableAppointmentSlot>();

        foreach (var schedule in schedules)
        {
            var slotStart = request.Date.ToDateTime(
                schedule.StartAt);

            var shiftEnd = request.Date.ToDateTime(
                schedule.EndAt);

            while (slotStart.AddMinutes(requiredMinutes)
                   <= shiftEnd)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var slotEnd = slotStart.AddMinutes(
                    requiredMinutes);

                var startUtc =
                    TimeZoneInfo.ConvertTimeToUtc(
                        slotStart, peruZone);

                var endUtc =
                    TimeZoneInfo.ConvertTimeToUtc(
                        slotEnd, peruZone);

                // Excluir el mismo horario original

                var sameOriginalSchedule =
                    startUtc == appointment.StartAt &&
                    endUtc == appointment.EndAt;

                // Conflictos del trabajador

                var employeeConflict =
                    employeeAppointments.Any(a =>
                        AppointmentRules.Overlaps(
                            startUtc,
                            endUtc,
                            a.StartAt,
                            a.EndAt));

                // Conflictos de la mascota

                var petConflict =
                    petAppointments.Any(a =>
                        AppointmentRules.Overlaps(
                            startUtc,
                            endUtc,
                            a.StartAt,
                            a.EndAt));

                // Bloqueos de agenda

                var blockConflict = blocks.Any(b =>
                    AppointmentRules.Overlaps(
                        startUtc,
                        endUtc,
                        b.StartAt,
                        b.EndAt));

                // Agregar únicamente horarios válidos

                if (startUtc > DateTime.UtcNow &&
                    !sameOriginalSchedule &&
                    !employeeConflict &&
                    !petConflict &&
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

                // Avanzar en intervalos de 15 minutos

                slotStart = slotStart.AddMinutes(
                    AppointmentRules.SlotIntervalMinutes);
            }
        }

        // 13. Retornar horarios ordenados

        return result
            .DistinctBy(x => x.StartAt)
            .OrderBy(x => x.StartAt)
            .ToList();
    }
}