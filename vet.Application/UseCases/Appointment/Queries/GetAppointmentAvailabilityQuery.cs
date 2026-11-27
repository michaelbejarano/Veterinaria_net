
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using AppointmentEntity = vet.Domain.Entities.Appointment;
using EmployeeEntity = vet.Domain.Entities.Employee;
using PetEntity = vet.Domain.Entities.Pet;
using ServiceEntity = vet.Domain.Entities.Service;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;
using EmployeeScheduleEntity = vet.Domain.Entities.EmployeeSchedule;
using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.Appointment.Queries;

public class GetAppointmentAvailabilityQuery
    : IRequest<List<AvailableAppointmentSlot>>
{
    public long EmployeeId { get; set; }

    public long? PetId { get; set; }

    public List<long> ServiceIds { get; set; } = new();

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
        // 1. Validar identificadores
        if (request.EmployeeId <= 0)
            throw new ArgumentException(
                "El trabajador es obligatorio.");

        if (request.ServiceIds is null ||
            request.ServiceIds.Count == 0)
            throw new ArgumentException(
                "Debe seleccionar al menos un servicio.");

        if (request.ServiceIds.Any(id => id <= 0))
            throw new ArgumentException(
                "Uno o más servicios son inválidos.");

        if (request.ServiceIds.Distinct().Count() !=
            request.ServiceIds.Count)
            throw new ArgumentException(
                "No puede repetir servicios.");

        // 2. Validar trabajador
        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null || !employee.IsActive)
            throw new ArgumentException(
                "El trabajador no existe o está inactivo.");

        // 3. Validar mascota cuando fue seleccionada
        if (request.PetId.HasValue)
        {
            if (request.PetId.Value <= 0)
                throw new ArgumentException(
                    "La mascota es inválida.");

            var pet = await _unitOfWork
                .Repository<PetEntity>()
                .GetByIdAsync(request.PetId.Value);

            if (pet is null || !pet.IsActive)
                throw new ArgumentException(
                    "La mascota no existe o está inactiva.");
        }

        // 4. Obtener servicios seleccionados
        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(s =>
                request.ServiceIds.Contains(s.Id)))
            .ToList();

        if (services.Count != request.ServiceIds.Count)
            throw new ArgumentException(
                "Uno o más servicios no existen.");

        // 5. Obtener asignaciones del trabajador
        var assignments = (await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.IsEnabled &&
                request.ServiceIds.Contains(x.ServiceId)))
            .ToList();

        foreach (var service in services)
        {
            if (!service.IsActive)
                throw new ArgumentException(
                    $"El servicio '{service.Name}' está inactivo.");

            if (service.DurationMinutes <= 0)
                throw new ArgumentException(
                    $"El servicio '{service.Name}' tiene una duración inválida.");

            if (!assignments.Any(x =>
                    x.ServiceId == service.Id))
                throw new ArgumentException(
                    $"El trabajador no tiene asignado el servicio '{service.Name}'.");

            if (service.IsClinical &&
                employee.Type != "VETERINARIAN")
                throw new ArgumentException(
                    $"El servicio '{service.Name}' requiere un veterinario.");
        }

        // 6. Calcular duración total
        var requiredMinutes = services.Sum(
            s => s.DurationMinutes);

        // 7. Configurar zona horaria de Perú
        var peruZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "America/Lima");

        var nowUtc = DateTime.UtcNow;

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            nowUtc, peruZone);

        if (request.Date < DateOnly.FromDateTime(localNow))
            return new List<AvailableAppointmentSlot>();

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

        // 8. Obtener horarios laborales
        var schedules = (await _unitOfWork
            .Repository<EmployeeScheduleEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.Weekday == weekday))
            .OrderBy(x => x.StartAt)
            .ToList();

        // 9. Obtener citas del trabajador
        var employeeAppointments = (await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.Status != "CANCELLED" &&
                x.Status != "EXPIRED" &&
                x.StartAt < dayEndUtc &&
                x.EndAt > dayStartUtc))
            .ToList();

        // 10. Obtener citas de la mascota
        var petAppointments = new List<AppointmentEntity>();

        if (request.PetId.HasValue)
        {
            var petId = request.PetId.Value;

            petAppointments = (await _unitOfWork
                .Repository<AppointmentEntity>()
                .FindAsync(x =>
                    x.PetId == petId &&
                    x.Status != "CANCELLED" &&
                    x.Status != "EXPIRED" &&
                    x.StartAt < dayEndUtc &&
                    x.EndAt > dayStartUtc))
                .ToList();
        }

        // 11. Obtener bloqueos de agenda
        var blocks = (await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
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

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                    slotStart, peruZone);

                var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                    slotEnd, peruZone);

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

                if (startUtc > DateTime.UtcNow &&
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

                // Avanzar 15 minutos, no la duración del servicio
                slotStart = slotStart.AddMinutes(
                    AppointmentRules.SlotIntervalMinutes);
            }
        }

        return result
            .DistinctBy(x => x.StartAt)
            .OrderBy(x => x.StartAt)
            .ToList();
    }
}
