using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using vet.Domain.Services.AppointmentReschedule;

using AppointmentRescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

namespace vet.Application.UseCases.AppointmentReschedule.Commands;

public class CancelAppointmentRescheduleCommand : IRequest<bool>
{
    public long RescheduleId { get; set; }
}

public class CancelAppointmentRescheduleCommandHandler
    : IRequestHandler<CancelAppointmentRescheduleCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentRescheduleCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        CancelAppointmentRescheduleCommand request,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Obtener repositorio

            var repository = _unitOfWork
                .Repository<AppointmentRescheduleEntity>();

            // 2. Buscar la reprogramación

            var initialReschedule = (await repository
                    .FindAsync(x => x.Id == request.RescheduleId))
                .SingleOrDefault();

            if (initialReschedule is null)
                throw new KeyNotFoundException(
                    "La reprogramación no existe.");

            // 3. Bloquear la cita original

            var appointment = await _unitOfWork
                .GetAppointmentForUpdateAsync(
                    initialReschedule.AppointmentId, ct);

            if (appointment is null)
                throw new KeyNotFoundException(
                    "La cita no existe.");

            // 4. Consultar nuevamente la solicitud pendiente

            var reschedule = await _unitOfWork
                .GetRescheduleForUpdateAsync(
                    request.RescheduleId, ct);

            if (reschedule is null ||
                reschedule.AppointmentId != appointment.Id ||
                reschedule.Status != "PENDING_PAYMENT")
            {
                throw new ArgumentException(
                    "La reprogramación no está pendiente de pago.");
            };

            // 5. Validar cancelación

            AppointmentRescheduleRules.ValidateCancellation(
                reschedule.Status);

            AppointmentRules.ValidatePaymentExpiration(
                reschedule.PaymentExpiresAt,
                DateTime.UtcNow);

            // 6. Cancelar únicamente la reprogramación

            reschedule.Status = "CANCELLED";

            // 7. Guardar cambios

            await _unitOfWork.SaveChangesAsync(ct);

        }, cancellationToken);

        return true;
    }
}