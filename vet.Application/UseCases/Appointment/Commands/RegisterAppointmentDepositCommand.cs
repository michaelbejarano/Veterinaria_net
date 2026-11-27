
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using PaymentEntity = vet.Domain.Entities.Payment;
using CashMovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.Appointment.Commands;

public class RegisterAppointmentDepositCommand : IRequest<long>
{
    public long AppointmentId { get; set; }
    public long CashSessionId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

public class RegisterAppointmentDepositCommandHandler
    : IRequestHandler<RegisterAppointmentDepositCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterAppointmentDepositCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        RegisterAppointmentDepositCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar identificadores
        if (request.AppointmentId <= 0)
            throw new ArgumentException(
                "La cita es obligatoria.");

        if (request.CashSessionId <= 0)
            throw new ArgumentException(
                "La sesión de caja es obligatoria.");

        long paymentId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                // 2. Bloquear la cita
                var appointment = await _unitOfWork
                    .GetAppointmentForUpdateAsync(
                        request.AppointmentId, ct);

                if (appointment is null)
                    throw new KeyNotFoundException(
                        "La cita no existe.");

                // 3. Bloquear la sesión de caja
                var cashSession = await _unitOfWork
                    .GetCashSessionForUpdateAsync(
                        request.CashSessionId, ct);

                if (cashSession is null)
                    throw new KeyNotFoundException(
                        "La sesión de caja no existe.");

                AppointmentRules.ValidateOpenCashSession(
                    cashSession.Status);

                // 4. Validar método de pago
                var method = request.Method?
                    .Trim()
                    .ToUpperInvariant()
                    ?? string.Empty;

                AppointmentRules.ValidatePaymentMethod(
                    method);

                // 5. Buscar pagos confirmados
                var payments = await _unitOfWork
                    .Repository<PaymentEntity>()
                    .FindAsync(p =>
                        p.AppointmentId == appointment.Id &&
                        p.Status == "CONFIRMED");

                // 6. Validar adelanto
                AppointmentRules.ValidateDepositPayment(
                    appointment.Status,
                    appointment.RequiredDeposit,
                    request.Amount,
                    payments.Any());

                // 7. Validar vencimiento del pago
                var now = DateTime.UtcNow;

                AppointmentRules.ValidatePaymentExpiration(
                    appointment.PaymentExpiresAt,
                    now);

                // 8. Validar que la cita no haya comenzado
                if (now >= appointment.StartAt)
                    throw new ArgumentException(
                        "No se puede registrar el adelanto porque la hora de inicio de la cita ya llegó.");

                var reference =
                    string.IsNullOrWhiteSpace(request.Reference)
                        ? null
                        : request.Reference.Trim();

                // 9. Crear pago
                var payment = new PaymentEntity
                {
                    AppointmentId = appointment.Id,
                    CashSessionId = cashSession.Id,
                    Amount = request.Amount,
                    Method = method,
                    Reference = reference,
                    Date = now,
                    Status = "CONFIRMED"
                };

                // 10. Crear movimiento de caja
                var movement = new CashMovementEntity
                {
                    CashSessionId = cashSession.Id,
                    Type = "INCOME",
                    Category = "PAYMENT",
                    Method = method,
                    Amount = request.Amount,
                    Reference = reference,
                    Description =
                        $"Adelanto de cita #{appointment.Id}",
                    Date = now,
                    Payment = payment
                };

                // 11. Confirmar cita
                appointment.Status = "CONFIRMED";

                // 12. Registrar entidades
                await _unitOfWork
                    .Repository<PaymentEntity>()
                    .AddAsync(payment);

                await _unitOfWork
                    .Repository<CashMovementEntity>()
                    .AddAsync(movement);

                // 13. Guardar cambios
                await _unitOfWork.SaveChangesAsync(ct);

                paymentId = payment.Id;

            }, cancellationToken);

        return paymentId;
    }
}
