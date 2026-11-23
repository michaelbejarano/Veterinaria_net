
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using AppointmentEntity = vet.Domain.Entities.Appointment;
using AppointmentServiceEntity = vet.Domain.Entities.AppointmentService;
using EmployeeEntity = vet.Domain.Entities.Employee;
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
        // 1. Normalizar las fechas a UTC
        var startUtc = request.StartAt.UtcDateTime;
        var endUtc = request.EndAt.UtcDateTime;

        AppointmentRules.ValidateDates(
            startUtc,
            endUtc,
            DateTime.UtcNow);

        // 2. Validar mascota
        var pet = await _unitOfWork
            .Repository<PetEntity>()
            .GetByIdAsync(request.PetId);

        if (pet is null)
            throw new ArgumentException("La mascota no existe.");

        if (!pet.IsActive)
            throw new ArgumentException("La mascota está inactiva.");

        // 3. Validar trabajador
        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null)
            throw new ArgumentException("El trabajador no existe.");

        if (!employee.IsActive)
            throw new ArgumentException("El trabajador está inactivo.");

        // 4. Validar lista de servicios
        if (request.ServiceIds is null || request.ServiceIds.Count == 0)
            throw new ArgumentException(
                "Debe seleccionar al menos un servicio.");

        if (request.ServiceIds.Distinct().Count() != request.ServiceIds.Count)
            throw new ArgumentException(
                "No puede repetir un servicio en la misma cita.");

        // 5. Buscar servicios y asignaciones del trabajador
        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .FindAsync(x => request.ServiceIds.Contains(x.Id)))
            .ToList();

        if (services.Count != request.ServiceIds.Count)
            throw new ArgumentException(
                "Uno o más servicios no existen.");

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

            if (!assignments.Any(x => x.ServiceId == service.Id))
                throw new ArgumentException(
                    $"El trabajador no tiene asignado el servicio '{service.Name}'.");
        }
        
        var requiredMinutes = services.Sum(
            service => service.DurationMinutes);

        AppointmentRules.ValidateDuration(
            startUtc,
            endUtc,
            requiredMinutes);


        // 6. Convertir a hora local de Perú
        var peruTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            "America/Lima");

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(
            startUtc, peruTimeZone);

        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(
            endUtc, peruTimeZone);

        short weekday = (short)(
            localStart.DayOfWeek == DayOfWeek.Sunday
                ? 7
                : (int)localStart.DayOfWeek);

        // 7. Verificar horario laboral
        var schedules = await _unitOfWork
            .Repository<EmployeeScheduleEntity>()
            .FindAsync(x => x.EmployeeId == request.EmployeeId);

        AppointmentRules.ValidateWorkingHours(
            localStart,
            localEnd,
            schedules,
            weekday);
        AppointmentRules.ValidateSlotAlignment(
            localStart,
            localEnd,
            schedules,
            weekday,
            requiredMinutes);

        // 8. Verificar bloqueos
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

        // 9. Verificar citas existentes
        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.StartAt < endUtc &&
                x.EndAt > startUtc);

        AppointmentRules.ValidateAppointmentConflicts(
            startUtc,
            endUtc,
            appointments);

        // 10. Calcular precio y adelanto
        var reservedPrice = services.Sum(x => x.BasePrice);

        var requiredDeposit =
            AppointmentRules.CalculateRequiredDeposit(reservedPrice);
        // Calcular el vencimiento del pago (15 minutos)
        var createdAt = DateTime.UtcNow;

        var paymentExpiresAt =
            AppointmentRules.CalculatePaymentExpiration(createdAt);

        // 11. Construir la cita y sus servicios
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
            CreatedAt = DateTime.UtcNow,

            AppointmentServices = services
                .Select(service => new AppointmentServiceEntity
                {
                    ServiceId = service.Id,
                    AgreedPrice = service.BasePrice
                })
                .ToList()
        };

        // 12. Guardar cita y servicios juntos
        await _unitOfWork
            .Repository<AppointmentEntity>()
            .AddAsync(appointment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}
