
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
        // 1. Buscar la cita
        var appointment = await _unitOfWork
            .Repository<AppointmentEntity>()
            .GetByIdAsync(request.AppointmentId);

        if (appointment is null)
            throw new KeyNotFoundException("La cita no existe.");

        // 2. Buscar la sesión de caja
        var cashSession = await _unitOfWork
            .Repository<CashSessionEntity>()
            .GetByIdAsync(request.CashSessionId);

        if (cashSession is null)
            throw new KeyNotFoundException(
                "La sesión de caja no existe.");

        AppointmentRules.ValidateOpenCashSession(
            cashSession.Status);

        // 3. Validar el método de pago
        var method = request.Method.Trim().ToUpperInvariant();

        AppointmentRules.ValidatePaymentMethod(method);

        // 4. Comprobar pagos anteriores
        var payments = await _unitOfWork
            .Repository<PaymentEntity>()
            .GetAllAsync();

        var hasConfirmedPayment = payments.Any(p =>
            p.AppointmentId == request.AppointmentId &&
            p.Status == "CONFIRMED");

        // 5. Validar el adelanto
        AppointmentRules.ValidateDepositPayment(
            appointment.Status,
            appointment.RequiredDeposit,
            request.Amount,
            hasConfirmedPayment);

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

        // 7. Crear el movimiento de caja
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

            // EF Core establecerá PaymentId al guardar.
            Payment = payment
        };

        // 8. Confirmar la cita
        appointment.Status = "CONFIRMED";

        // 9. Registrar las entidades
        await _unitOfWork.Repository<PaymentEntity>()
            .AddAsync(payment);

        await _unitOfWork.Repository<CashMovementEntity>()
            .AddAsync(movement);

        // 10. Guardar las tres operaciones juntas
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
