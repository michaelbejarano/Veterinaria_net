
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using AppointmentEntity = vet.Domain.Entities.Appointment;
using PaymentEntity = vet.Domain.Entities.Payment;
using CashMovementEntity = vet.Domain.Entities.CashMovement;
using CashSessionEntity = vet.Domain.Entities.CashSession;

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
    long paymentId = 0;

    await _unitOfWork.ExecuteInTransactionAsync(async ct =>
    {
        // 1. Bloquear la cita dentro de la transacción
        var appointment = await _unitOfWork
            .GetAppointmentForUpdateAsync(request.AppointmentId, ct);

        if (appointment is null)
            throw new KeyNotFoundException("La cita no existe.");

        // 2. Verificar la sesión de caja
        var cashSession = await _unitOfWork
            .Repository<CashSessionEntity>()
            .GetByIdAsync(request.CashSessionId);

        if (cashSession is null)
            throw new KeyNotFoundException(
                "La sesión de caja no existe.");

        AppointmentRules.ValidateOpenCashSession(
            cashSession.Status);

        // 3. Validar el método de pago
        var method = request.Method?.Trim().ToUpperInvariant()
            ?? string.Empty;

        AppointmentRules.ValidatePaymentMethod(method);

        // 4. Consultar pagos confirmados de la cita
        var payments = await _unitOfWork
            .Repository<PaymentEntity>()
            .FindAsync(p =>
                p.AppointmentId == appointment.Id &&
                p.Status == "CONFIRMED");

        var hasConfirmedPayment = payments.Any();

        // 5. Validar las reglas del adelanto
        AppointmentRules.ValidateDepositPayment(
            appointment.Status,
            appointment.RequiredDeposit,
            request.Amount,
            hasConfirmedPayment);
        //valdimao el vencimiento 15 m r que no hayan transcurrido los 15 minutos

        AppointmentRules.ValidatePaymentExpiration(
            appointment.PaymentExpiresAt,
            DateTime.UtcNow);

        // 6. Crear el pago
        var payment = new PaymentEntity
        {
            AppointmentId = appointment.Id,
            CashSessionId = cashSession.Id,
            Amount = request.Amount,
            Method = method,
            Reference = request.Reference,
            Date = DateTime.UtcNow,
            Status = "CONFIRMED"
        };

        // 7. Crear movimiento de caja
        var movement = new CashMovementEntity
        {
            CashSessionId = cashSession.Id,
            Type = "INCOME",
            Category = "PAYMENT",
            Method = method,
            Amount = request.Amount,
            Reference = request.Reference,
            Description = $"Adelanto de cita #{appointment.Id}",
            Date = DateTime.UtcNow,
            Payment = payment
        };

        // 8. Confirmar la cita
        appointment.Status = "CONFIRMED";

        // 9. Registrar entidades
        await _unitOfWork.Repository<PaymentEntity>()
            .AddAsync(payment);

        await _unitOfWork.Repository<CashMovementEntity>()
            .AddAsync(movement);

        // 10. Guardar para obtener el ID generado
        await _unitOfWork.SaveChangesAsync(ct);

        paymentId = payment.Id;

    }, cancellationToken);

    return paymentId;
}

}
