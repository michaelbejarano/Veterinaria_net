using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.AppointmentReschedule;

using AppointmentRescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

using AppointmentEntity =
    vet.Domain.Entities.Appointment;

using EmployeeEntity =
    vet.Domain.Entities.Employee;

using EmployeeScheduleEntity =
    vet.Domain.Entities.EmployeeSchedule;

using ScheduleBlockEntity =
    vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.AppointmentReschedule.Commands;

public class CreateAppointmentRescheduleCommand : IRequest<long>
{
    public long AppointmentId { get; set; }

    public DateTimeOffset NewStart { get; set; }

    public DateTimeOffset NewEnd { get; set; }
}

public class CreateAppointmentRescheduleCommandHandler
    : IRequestHandler<CreateAppointmentRescheduleCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAppointmentRescheduleCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateAppointmentRescheduleCommand request,
        CancellationToken cancellationToken)
    {
        long rescheduleId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Normalizar fechas a UTC

            var newStartUtc = request.NewStart.UtcDateTime;
            var newEndUtc = request.NewEnd.UtcDateTime;

            // 2. Buscar y bloquear la cita

            var appointment = await _unitOfWork
                .GetAppointmentForUpdateAsync(
                    request.AppointmentId, ct);

            if (appointment is null)
                throw new KeyNotFoundException(
                    "La cita no existe.");

            // 3. Buscar reprogramaciones pendientes

            var pendingReschedules = await _unitOfWork
                .Repository<AppointmentRescheduleEntity>()
                .FindAsync(x =>
                    x.AppointmentId == appointment.Id &&
                    x.Status == "PENDING_PAYMENT");

            var hasPendingReschedule =
                pendingReschedules.Any();

            // 4. Validar reglas de negocio

            AppointmentRescheduleRules.ValidateRequest(
                appointment.Status,
                appointment.StartAt,
                appointment.EndAt,
                newStartUtc,
                newEndUtc,
                DateTime.UtcNow,
                hasPendingReschedule);

            // 5. Validar trabajador

            var employee = await _unitOfWork
                .GetEmployeeForUpdateAsync(
                    appointment.EmployeeId, ct);

            if (employee is null)
                throw new KeyNotFoundException(
                    "El trabajador no existe.");

            if (!employee.IsActive)
                throw new ArgumentException(
                    "El trabajador está inactivo.");

            // 6. Conservar duración original

            var requiredMinutes = (int)(
                appointment.EndAt -
                appointment.StartAt).TotalMinutes;

            AppointmentRules.ValidateDuration(
                newStartUtc,
                newEndUtc,
                requiredMinutes);

            // 7. Convertir fechas a hora local de Perú

            var peruTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Lima");

            var localStart =
                TimeZoneInfo.ConvertTimeFromUtc(
                    newStartUtc, peruTimeZone);

            var localEnd =
                TimeZoneInfo.ConvertTimeFromUtc(
                    newEndUtc, peruTimeZone);

            short weekday = (short)(
                localStart.DayOfWeek == DayOfWeek.Sunday
                    ? 7
                    : (int)localStart.DayOfWeek);

            // 8. Validar horario laboral

            var schedules = await _unitOfWork
                .Repository<EmployeeScheduleEntity>()
                .FindAsync(x =>
                    x.EmployeeId == appointment.EmployeeId);

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

            // 9. Validar bloqueos de agenda

            var blocks = await _unitOfWork
                .Repository<ScheduleBlockEntity>()
                .FindAsync(x =>
                    x.EmployeeId == appointment.EmployeeId &&
                    x.StartAt < newEndUtc &&
                    x.EndAt > newStartUtc);

            AppointmentRules.ValidateScheduleBlocks(
                newStartUtc,
                newEndUtc,
                blocks);

            // 10. Validar conflictos con otras citas

            var appointments = await _unitOfWork
                .Repository<AppointmentEntity>()
                .FindAsync(x =>
                    x.Id != appointment.Id &&
                    x.EmployeeId == appointment.EmployeeId &&
                    x.StartAt < newEndUtc &&
                    x.EndAt > newStartUtc);

            AppointmentRules.ValidateAppointmentConflicts(
                newStartUtc,
                newEndUtc,
                appointments);
            
        // Validar conflictos de horario de la mascota
        var petAppointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.Id != appointment.Id &&
                x.PetId == appointment.PetId &&
                x.StartAt < newEndUtc &&
                x.EndAt > newStartUtc);

        AppointmentRules.ValidatePetConflicts(
            newStartUtc,
            newEndUtc,
            petAppointments);

            // 11. Calcular vencimiento del pago

            var createdAt = DateTime.UtcNow;

            var paymentExpiresAt =
                AppointmentRescheduleRules
                    .CalculatePaymentExpiration(
                        createdAt,
                        appointment.StartAt,
                        newStartUtc);

            // 12. Crear solicitud de reprogramación

            var reschedule = new AppointmentRescheduleEntity
            {
                AppointmentId = appointment.Id,

                PreviousStart = appointment.StartAt,
                PreviousEnd = appointment.EndAt,

                NewStart = newStartUtc,
                NewEnd = newEndUtc,

                Fee = AppointmentRescheduleRules.Fee,

                Status = "PENDING_PAYMENT",

                PaymentExpiresAt = paymentExpiresAt,

                CreatedAt = createdAt
            };

            // 13. Guardar solicitud

            await _unitOfWork
                .Repository<AppointmentRescheduleEntity>()
                .AddAsync(reschedule);

            await _unitOfWork.SaveChangesAsync(ct);

            rescheduleId = reschedule.Id;

        }, cancellationToken);

        return rescheduleId;
    }
}