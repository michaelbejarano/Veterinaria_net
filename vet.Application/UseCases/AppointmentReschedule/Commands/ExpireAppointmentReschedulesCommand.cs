using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.AppointmentReschedule;

using AppointmentRescheduleEntity =
    vet.Domain.Entities.AppointmentReschedule;

namespace vet.Application.UseCases.AppointmentReschedule.Commands;

public class ExpireAppointmentReschedulesCommand : IRequest<int>
{
}

public class ExpireAppointmentReschedulesCommandHandler
    : IRequestHandler<ExpireAppointmentReschedulesCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpireAppointmentReschedulesCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        ExpireAppointmentReschedulesCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // 1. Obtener repositorio

        var repository = _unitOfWork
            .Repository<AppointmentRescheduleEntity>();

        // 2. Buscar solicitudes pendientes vencidas

        var expiredReschedules = (await repository
            .FindAsync(x =>
                x.Status == "PENDING_PAYMENT" &&
                x.PaymentExpiresAt.HasValue &&
                x.PaymentExpiresAt.Value <= now))
            .OrderBy(x => x.AppointmentId)
            .ToList();

        int expiredCount = 0;

        // 3. Procesar cada solicitud

        foreach (var candidate in expiredReschedules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool wasExpired = false;

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // 4. Bloquear la cita original

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        candidate.AppointmentId, ct);

                if (appointment is null)
                    return;

                // 5. Consultar nuevamente la solicitud

                var reschedule = await _unitOfWork
                    .GetRescheduleForUpdateAsync(candidate.Id, ct);
                if (reschedule is null ||
                    reschedule.AppointmentId != appointment.Id ||
                    reschedule.Status != "PENDING_PAYMENT")
                {
                    return;
                }

                // 6. Validar nuevamente el vencimiento

                if (!AppointmentRescheduleRules.ShouldExpire(
                        reschedule.Status,
                        reschedule.PaymentExpiresAt,
                        DateTime.UtcNow))
                {
                    return;
                }

                // 7. Actualizar estado

                reschedule.Status = "EXPIRED";

                // 8. Guardar cambios

                await _unitOfWork.SaveChangesAsync(ct);

                wasExpired = true;

            }, cancellationToken);

            if (wasExpired)
                expiredCount++;
        }

        // 9. Retornar cantidad de solicitudes vencidas

        return expiredCount;
    }
}