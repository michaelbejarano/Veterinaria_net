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

public class MarkAppointmentNoShowCommand
    : IRequest<MarkAppointmentNoShowResponse>
{
    public long AppointmentId { get; set; }
}

public class MarkAppointmentNoShowResponse
{
    public long AppointmentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal ForfeitedDeposit { get; set; }
}

public class MarkAppointmentNoShowCommandHandler
    : IRequestHandler<
        MarkAppointmentNoShowCommand,
        MarkAppointmentNoShowResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkAppointmentNoShowCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkAppointmentNoShowResponse> Handle(
        MarkAppointmentNoShowCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        MarkAppointmentNoShowResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 1. Bloquear cita

                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId, ct);

                if (appointment is null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                var now = DateTime.UtcNow;

                // 2. Validar inasistencia

                AppointmentRules.ValidateNoShow(
                    appointment.Status,
                    appointment.EndAt,
                    now);

                // 3. Buscar adelantos confirmados

                var payments = (await _unitOfWork
                    .Repository<PaymentEntity>()
                    .FindAsync(p =>
                        p.AppointmentId == appointment.Id &&
                        p.Status == "CONFIRMED"))
                    .ToList();

                if (!payments.Any())
                    throw new ArgumentException(
                        "La cita no tiene un adelanto confirmado.");

                decimal forfeitedDeposit = 0m;

                // 4. Registrar retención

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
                        applications.Any() ? payment.Amount : 0m);

                    var forfeiture = new DepositForfeitureEntity
                    {
                        PaymentId = payment.Id,
                        AppointmentId = appointment.Id,
                        Amount = payment.Amount,
                        Reason = "NO_SHOW",
                        Date = now
                    };

                    await _unitOfWork
                        .Repository<DepositForfeitureEntity>()
                        .AddAsync(forfeiture);

                    forfeitedDeposit += payment.Amount;
                }

                // 5. Finalizar reprogramaciones pendientes

                var reschedules = await _unitOfWork
                    .Repository<RescheduleEntity>()
                    .FindAsync(r =>
                        r.AppointmentId == appointment.Id &&
                        r.Status == "PENDING_PAYMENT");

                foreach (var reschedule in reschedules)
                {
                    reschedule.Status =
                        AppointmentRules.ShouldExpire(
                            reschedule.Status,
                            reschedule.PaymentExpiresAt,
                            now)
                            ? "EXPIRED"
                            : "CANCELLED";
                }

                // 6. Actualizar estado de cita

                appointment.Status = "NO_SHOW";

                await _unitOfWork.SaveChangesAsync(ct);

                response = new MarkAppointmentNoShowResponse
                {
                    AppointmentId = appointment.Id,
                    Status = appointment.Status,
                    ForfeitedDeposit = forfeitedDeposit
                };
            },
            cancellationToken);

        return response!;
    }
}