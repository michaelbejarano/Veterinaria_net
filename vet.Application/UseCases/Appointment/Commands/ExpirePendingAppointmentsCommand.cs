
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using AppointmentEntity = vet.Domain.Entities.Appointment;

namespace vet.Application.UseCases.Appointment.Commands;

public class ExpirePendingAppointmentsCommand : IRequest<int>
{
}

public class ExpirePendingAppointmentsCommandHandler
    : IRequestHandler<ExpirePendingAppointmentsCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpirePendingAppointmentsCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        ExpirePendingAppointmentsCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        // 1. Buscar citas pendientes que deben expirar
        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.Status == "PENDING_PAYMENT" &&
                (
                    (x.PaymentExpiresAt != null &&
                     x.PaymentExpiresAt <= nowUtc)
                    ||
                    x.StartAt <= nowUtc
                ));

        int expiredCount = 0;

        // 2. Procesar cada cita dentro de una transacción
        foreach (var candidate in appointments)
        {
            bool wasExpired = false;

            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    // 3. Bloquear la cita
                    var appointment = await _unitOfWork
                        .GetAppointmentForUpdateAsync(
                            candidate.Id, ct);

                    if (appointment is null)
                        return;

                    // 4. Verificar estado actualizado
                    if (appointment.Status != "PENDING_PAYMENT")
                        return;

                    // 5. Obtener hora actual
                    var currentUtc = DateTime.UtcNow;

                    // 6. Validar vencimiento del pago
                    var hasExpired = AppointmentRules.ShouldExpire(
                        appointment.Status,
                        appointment.PaymentExpiresAt,
                        currentUtc);

                    // 7. Validar hora de inicio
                    var hasReachedStart =
                        appointment.StartAt <= currentUtc;

                    // 8. Conservar la reserva si sigue vigente
                    if (!hasExpired && !hasReachedStart)
                        return;

                    // 9. Expirar la cita
                    appointment.Status = "EXPIRED";

                    // 10. Guardar cambios
                    await _unitOfWork.SaveChangesAsync(ct);

                    wasExpired = true;

                }, cancellationToken);

            // 11. Contabilizar después de confirmar la transacción
            if (wasExpired)
                expiredCount++;
        }

        return expiredCount;
    }
}
