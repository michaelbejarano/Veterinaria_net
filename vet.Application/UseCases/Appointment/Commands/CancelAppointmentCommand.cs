
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using PaymentEntity = vet.Domain.Entities.Payment;

using DepositForfeitureEntity =
    vet.Domain.Entities.DepositForfeiture;

using DepositApplicationEntity =
    vet.Domain.Entities.DepositApplication;

using RescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

namespace vet.Application.UseCases.Appointment.Commands;

public class CancelAppointmentCommand : IRequest<bool>
{
    public long AppointmentId { get; set; }
}

public class CancelAppointmentCommandHandler
    : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        CancelAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Obtener y bloquear la cita
                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId, ct);

                if (appointment is null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                // 2. Validar cancelación
                AppointmentRules.ValidateCancellation(
                    appointment.Status);

                var now = DateTime.UtcNow;

                // 3. Buscar adelantos confirmados
                var payments = (await _unitOfWork
                    .Repository<PaymentEntity>()
                    .FindAsync(p =>
                        p.AppointmentId == appointment.Id &&
                        p.Status == "CONFIRMED"))
                    .ToList();

                if (appointment.Status == "CONFIRMED" &&
                    !payments.Any())
                {
                    throw new ArgumentException(
                        "La cita confirmada no tiene un adelanto registrado.");
                }

                // 4. Registrar retención de adelantos
                foreach (var payment in payments)
                {
                    var forfeitures = await _unitOfWork
                        .Repository<DepositForfeitureEntity>()
                        .FindAsync(f =>
                            f.PaymentId == payment.Id);

                    var applications = await _unitOfWork
                        .Repository<DepositApplicationEntity>()
                        .FindAsync(a =>
                            a.PaymentId == payment.Id);

                    AppointmentRules.ValidateDepositForfeiture(
                        payment.Amount,
                        forfeitures.Any(),
                        applications.Sum(a => a.Amount));

                    var forfeiture =
                        new DepositForfeitureEntity
                        {
                            PaymentId = payment.Id,
                            AppointmentId = appointment.Id,
                            Amount = payment.Amount,
                            Reason = "CANCELLATION",
                            Date = now
                        };

                    await _unitOfWork
                        .Repository<DepositForfeitureEntity>()
                        .AddAsync(forfeiture);
                }

                // 5. Buscar reprogramaciones pendientes
                var pendingReschedules = (await _unitOfWork
                    .Repository<RescheduleEntity>()
                    .FindAsync(r =>
                        r.AppointmentId == appointment.Id &&
                        r.Status == "PENDING_PAYMENT"))
                    .OrderBy(r => r.Id)
                    .ToList();

                // 6. Finalizar reprogramaciones pendientes
                foreach (var pending in pendingReschedules)
                {
                    var reschedule = await _unitOfWork
                        .GetRescheduleForUpdateAsync(
                            pending.Id, ct);

                    if (reschedule is null ||
                        reschedule.Status != "PENDING_PAYMENT")
                        continue;

                    reschedule.Status =
                        AppointmentRules.ShouldExpire(
                            reschedule.Status,
                            reschedule.PaymentExpiresAt,
                            now)
                            ? "EXPIRED"
                            : "CANCELLED";
                }

                // 7. Cancelar cita
                appointment.Status = "CANCELLED";

                // 8. Guardar todos los cambios
                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        return true;
    }
}
