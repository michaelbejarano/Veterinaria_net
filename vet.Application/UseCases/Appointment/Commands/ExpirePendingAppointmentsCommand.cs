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

        // Buscar únicamente las citas cuyo plazo terminó
        var appointments = await _unitOfWork
            .Repository<AppointmentEntity>()
            .FindAsync(x =>
                x.Status == "PENDING_PAYMENT" &&
                x.PaymentExpiresAt != null &&
                x.PaymentExpiresAt <= nowUtc);

        int expiredCount = 0;

        foreach (var candidate in appointments)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // Bloquear la cita y consultar su estado actualizado
                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(candidate.Id, ct);

                if (appointment is null)
                    return;

                // Recalcular la hora después de adquirir el bloqueo
                var currentUtc = DateTime.UtcNow;

                if (!AppointmentRules.ShouldExpire(
                        appointment.Status,
                        appointment.PaymentExpiresAt,
                        currentUtc))
                    return;

                appointment.Status = "EXPIRED";

                await _unitOfWork.SaveChangesAsync(ct);

                expiredCount++;

            }, cancellationToken);
        }

        return expiredCount;
    

    }
}