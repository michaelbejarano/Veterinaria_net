
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using AppointmentEntity = vet.Domain.Entities.Appointment;
using AppointmentServiceEntity = vet.Domain.Entities.AppointmentService;
using PetEntity = vet.Domain.Entities.Pet;
using ServiceEntity = vet.Domain.Entities.Service;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;
using EmployeeScheduleEntity = vet.Domain.Entities.EmployeeSchedule;
using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.Appointment.Commands;

public class CreateAppointmentCommand : IRequest<long>
{
    public long PetId { get; set; }
    public long EmployeeId { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }

    public List<long> ServiceIds { get; set; } = new();
}

public class CreateAppointmentCommandHandler
    : IRequestHandler<CreateAppointmentCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAppointmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar identificadores
        if (request.PetId <= 0)
            throw new ArgumentException(
                "La mascota es obligatoria.");

        if (request.EmployeeId <= 0)
            throw new ArgumentException(
                "El trabajador es obligatorio.");

        // 2. Normalizar fechas a UTC
        var startUtc = request.StartAt.UtcDateTime;
        var endUtc = request.EndAt.UtcDateTime;

        AppointmentRules.ValidateDates(
            startUtc,
            endUtc,
            DateTime.UtcNow);

        // 3. Validar servicios solicitados
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
                "No puede repetir un servicio en la misma cita.");

        long appointmentId = 0;

        // 4. Ejecutar reserva dentro de una transacción
        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 5. Obtener y bloquear trabajador
                var employee = await _unitOfWork
                    .GetEmployeeForUpdateAsync(
                        request.EmployeeId, ct);

                if (employee is null)
                    throw new ArgumentException(
                        "El trabajador no existe.");

                if (!employee.IsActive)
                    throw new ArgumentException(
                        "El trabajador está inactivo.");

                // 6. Validar mascota
                var pet = await _unitOfWork
                    .Repository<PetEntity>()
                    .GetByIdAsync(request.PetId);

                if (pet is null)
                    throw new ArgumentException(
                        "La mascota no existe.");

                if (!pet.IsActive)
                    throw new ArgumentException(
                        "La mascota está inactiva.");

                // 7. Obtener servicios
                var services = (await _unitOfWork
                    .Repository<ServiceEntity>()
                    .FindAsync(s =>
                        request.ServiceIds.Contains(s.Id)))
                    .ToList();

                if (services.Count != request.ServiceIds.Count)
                    throw new ArgumentException(
                        "Uno o más servicios no existen.");

                // 8. Obtener asignaciones del trabajador
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

                    if (!assignments.Any(x =>
                            x.ServiceId == service.Id))
                        throw new ArgumentException(
                            $"El trabajador no tiene asignado el servicio '{service.Name}'.");

                    if (service.IsClinical &&
                        employee.Type != "VETERINARIAN")
                        throw new ArgumentException(
                            $"El servicio '{service.Name}' requiere un veterinario.");
                }

                // 9. Calcular duración total
                var requiredMinutes = services.Sum(
                    s => s.DurationMinutes);

                AppointmentRules.ValidateDuration(
                    startUtc,
                    endUtc,
                    requiredMinutes);

                // 10. Convertir a hora local de Perú
                var peruTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "America/Lima");

                var localStart =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        startUtc, peruTimeZone);

                var localEnd =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        endUtc, peruTimeZone);

                short weekday = (short)(
                    localStart.DayOfWeek == DayOfWeek.Sunday
                        ? 7
                        : (int)localStart.DayOfWeek);

                // 11. Validar horario laboral
                var schedules = (await _unitOfWork
                    .Repository<EmployeeScheduleEntity>()
                    .FindAsync(x =>
                        x.EmployeeId == request.EmployeeId))
                    .ToList();

                AppointmentRules.ValidateWorkingHours(
                    localStart,
                    localEnd,
                    schedules,
                    weekday);

                // 12. Validar intervalos de 15 minutos
                AppointmentRules.ValidateSlotAlignment(
                    localStart,
                    localEnd,
                    schedules,
                    weekday,
                    requiredMinutes);

                // 13. Validar bloqueos de agenda
                var blocks = await _unitOfWork
                    .Repository<ScheduleBlockEntity>()
                    .FindAsync(x =>
                        x.EmployeeId == request.EmployeeId &&
                        x.StartAt < endUtc &&
                        x.EndAt > startUtc);

                AppointmentRules.ValidateScheduleBlocks(
                    startUtc,
                    endUtc,
                    blocks);

                // 14. Validar disponibilidad del trabajador
                var employeeAppointments = await _unitOfWork
                    .Repository<AppointmentEntity>()
                    .FindAsync(x =>
                        x.EmployeeId == request.EmployeeId &&
                        x.StartAt < endUtc &&
                        x.EndAt > startUtc);

                AppointmentRules.ValidateAppointmentConflicts(
                    startUtc,
                    endUtc,
                    employeeAppointments);

                // 15. Validar disponibilidad de la mascota
                var petAppointments = await _unitOfWork
                    .Repository<AppointmentEntity>()
                    .FindAsync(x =>
                        x.PetId == request.PetId &&
                        x.StartAt < endUtc &&
                        x.EndAt > startUtc);

                AppointmentRules.ValidatePetConflicts(
                    startUtc,
                    endUtc,
                    petAppointments);

                // 16. Calcular precio y adelanto
                var reservedPrice = services.Sum(
                    x => x.BasePrice);

                var requiredDeposit =
                    AppointmentRules.CalculateRequiredDeposit(
                        reservedPrice);

                var createdAt = DateTime.UtcNow;

                // Verificar que el horario siga siendo futuro
                AppointmentRules.ValidateDates(
                    startUtc,
                    endUtc,
                    createdAt);

                var paymentExpiresAt =
                    AppointmentRules.CalculatePaymentExpiration(createdAt);

                if (paymentExpiresAt > startUtc)
                {
                    paymentExpiresAt = startUtc;
                }

                // 17. Construir la cita
                var appointment = new AppointmentEntity
                {
                    PetId = request.PetId,
                    EmployeeId = request.EmployeeId,
                    StartAt = startUtc,
                    EndAt = endUtc,
                    Status = "PENDING_PAYMENT",
                    ReservedPrice = reservedPrice,
                    DepositPercent = AppointmentRules.DepositPercent,
                    RequiredDeposit = requiredDeposit,
                    PaymentExpiresAt = paymentExpiresAt,
                    CreatedAt = createdAt,

                    AppointmentServices = services
                        .Select(service =>
                            new AppointmentServiceEntity
                            {
                                ServiceId = service.Id,
                                AgreedPrice = service.BasePrice
                            })
                        .ToList()
                };

                // 18. Guardar cita y servicios
                await _unitOfWork
                    .Repository<AppointmentEntity>()
                    .AddAsync(appointment);

                await _unitOfWork.SaveChangesAsync(ct);

                appointmentId = appointment.Id;

            }, cancellationToken);

        return appointmentId;
    }
}
