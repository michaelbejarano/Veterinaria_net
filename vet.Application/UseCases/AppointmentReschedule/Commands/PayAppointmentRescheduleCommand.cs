using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.AppointmentReschedule;

using AppointmentEntity =
    vet.Domain.Entities.Appointment;

using AppointmentRescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

using PaymentEntity =
    vet.Domain.Entities.Payment;

using CashMovementEntity =
    vet.Domain.Entities.CashMovement;

using EmployeeEntity =
    vet.Domain.Entities.Employee;

using EmployeeScheduleEntity =
    vet.Domain.Entities.EmployeeSchedule;

using ScheduleBlockEntity =
    vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.AppointmentReschedule.Commands;

public class PayAppointmentRescheduleCommand : IRequest<long>
{
    public long RescheduleId { get; set; }

    public long CashSessionId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public string? Reference { get; set; }
}

public class PayAppointmentRescheduleCommandHandler
    : IRequestHandler<PayAppointmentRescheduleCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public PayAppointmentRescheduleCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        PayAppointmentRescheduleCommand request,
        CancellationToken cancellationToken)
    {
        long paymentId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Buscar la solicitud de reprogramación

            var rescheduleRepository = _unitOfWork
                .Repository<AppointmentRescheduleEntity>();

            var initialReschedule = (await rescheduleRepository
                    .FindAsync(x => x.Id == request.RescheduleId))
                .SingleOrDefault();

            if (initialReschedule is null)
                throw new KeyNotFoundException(
                    "La reprogramación no existe.");

            // 2. Bloquear la cita original

            var appointment = await _unitOfWork
                .GetAppointmentForUpdateAsync(
                    initialReschedule.AppointmentId, ct);

            if (appointment is null)
                throw new KeyNotFoundException(
                    "La cita no existe.");

            // 3. Consultar nuevamente la solicitud
            // después de bloquear la cita

            var reschedule = await _unitOfWork
                .GetRescheduleForUpdateAsync(
                    request.RescheduleId, ct);

            if (reschedule is null ||
                reschedule.AppointmentId != appointment.Id ||
                reschedule.Status != "PENDING_PAYMENT")
            {
                throw new ArgumentException(
                    "La reprogramación no está pendiente de pago.");
            }

            // 4. Bloquear y validar la sesión de caja

            var cashSession = await _unitOfWork
                .GetCashSessionForUpdateAsync(
                    request.CashSessionId, ct);

            if (cashSession is null)
                throw new KeyNotFoundException(
                    "La sesión de caja no existe.");

            AppointmentRules.ValidateOpenCashSession(
                cashSession.Status);

            // 5. Validar método de pago

            var method = request.Method?
                .Trim()
                .ToUpperInvariant() ?? string.Empty;

            AppointmentRules.ValidatePaymentMethod(method);

            // 6. Verificar pagos existentes

            var payments = await _unitOfWork
                .Repository<PaymentEntity>()
                .FindAsync(x =>
                    x.RescheduleId == reschedule.Id &&
                    x.Status == "CONFIRMED");

            // 7. Validar reglas del pago

            AppointmentRescheduleRules.ValidatePayment(
                appointment.Status,
                reschedule.Status,
                reschedule.Fee,
                request.Amount,
                reschedule.PaymentExpiresAt,
                DateTime.UtcNow,
                payments.Any());

            // 8. Verificar que la cita original
            // no haya cambiado de horario

            if (appointment.StartAt != reschedule.PreviousStart ||
                appointment.EndAt != reschedule.PreviousEnd)
            {
                throw new ArgumentException(
                    "El horario original de la cita ha cambiado.");
            }

            if (appointment.StartAt <= DateTime.UtcNow)
                throw new ArgumentException(
                    "La hora de la cita original ya llegó.");

            // 9. Validar las nuevas fechas

            var newStartUtc = reschedule.NewStart;
            var newEndUtc = reschedule.NewEnd;

            AppointmentRules.ValidateDates(
                newStartUtc,
                newEndUtc,
                DateTime.UtcNow);

            var requiredMinutes = (int)(
                appointment.EndAt -
                appointment.StartAt).TotalMinutes;

            AppointmentRules.ValidateDuration(
                newStartUtc,
                newEndUtc,
                requiredMinutes);

            // 10. Verificar trabajador

            var employee = await _unitOfWork
                .GetEmployeeForUpdateAsync(
                    appointment.EmployeeId, ct);

            if (employee is null || !employee.IsActive)
                throw new ArgumentException(
                    "El trabajador no está disponible.");

            // 11. Convertir a hora local de Perú

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

            // 12. Validar horario laboral

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

            // 13. Verificar bloqueos de agenda

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

            // 14. Verificar conflictos con otras citas

            var appointments = await _unitOfWork
                .Repository<AppointmentEntity>()
                .FindAsync(x =>
                    x.Id != appointment.Id &&
                    x.EmployeeId == appointment.EmployeeId &&
                    x.StartAt < newEndUtc &&
                    x.EndAt > newStartUtc);

            // 14.1. Verificar conflictos de horario de la mascota
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

            // 15. Verificar nuevamente el vencimiento

            var now = DateTime.UtcNow;

            AppointmentRules.ValidatePaymentExpiration(
                reschedule.PaymentExpiresAt,
                now);

            // 16. Registrar pago de reprogramación

            var payment = new PaymentEntity
            {
                RescheduleId = reschedule.Id,
                CashSessionId = cashSession.Id,
                Amount = request.Amount,
                Method = method,
                Reference = request.Reference,
                Date = now,
                Status = "CONFIRMED"
            };

            // 17. Registrar ingreso en caja

            var movement = new CashMovementEntity
            {
                CashSessionId = cashSession.Id,
                Type = "INCOME",
                Category = "PAYMENT",
                Method = method,
                Amount = request.Amount,
                Reference = request.Reference,
                Description =
                    $"Reprogramación de cita #{appointment.Id}",
                Date = now,
                Payment = payment
            };

            // 18. Actualizar horario de la cita

            appointment.StartAt = reschedule.NewStart;
            appointment.EndAt = reschedule.NewEnd;

            // La cita permanece CONFIRMED

            // 19. Aprobar la reprogramación

            reschedule.Status = "APPROVED";

            // 20. Registrar entidades

            await _unitOfWork
                .Repository<PaymentEntity>()
                .AddAsync(payment);

            await _unitOfWork
                .Repository<CashMovementEntity>()
                .AddAsync(movement);

            // 21. Guardar todos los cambios

            await _unitOfWork.SaveChangesAsync(ct);

            paymentId = payment.Id;

        }, cancellationToken);

        return paymentId;
    }
}