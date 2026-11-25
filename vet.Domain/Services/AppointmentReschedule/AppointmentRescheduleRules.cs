namespace vet.Domain.Services.AppointmentReschedule;

public static class AppointmentRescheduleRules
{
    public const decimal Fee = 10m;

    public static void ValidateRequest(
        string appointmentStatus,
        DateTime previousStart,
        DateTime previousEnd,
        DateTime newStart,
        DateTime newEnd,
        DateTime now,
        bool hasPendingReschedule)
    {
        if (appointmentStatus != "CONFIRMED")
            throw new ArgumentException(
                "Solo se pueden reprogramar citas confirmadas.");

        if (previousStart <= now)
            throw new ArgumentException(
                "No se puede reprogramar una cita cuya hora ya llegó.");

        if (hasPendingReschedule)
            throw new ArgumentException(
                "La cita ya tiene una reprogramación pendiente.");

        AppointmentRules.ValidateDates(
            newStart, newEnd, now);

        if (previousStart == newStart &&
            previousEnd == newEnd)
            throw new ArgumentException(
                "Debe seleccionar un horario diferente.");

        if (newEnd - newStart != previousEnd - previousStart)
            throw new ArgumentException(
                "Debe conservar la duración original de la cita.");
    }

    public static void ValidatePayment(
        string appointmentStatus,
        string rescheduleStatus,
        decimal fee,
        decimal amount,
        DateTime? expiresAt,
        DateTime now,
        bool hasConfirmedPayment)
    {
        if (appointmentStatus != "CONFIRMED")
            throw new ArgumentException(
                "La cita debe estar confirmada.");

        if (rescheduleStatus != "PENDING_PAYMENT")
            throw new ArgumentException(
                "La reprogramación no está pendiente de pago.");

        if (fee <= 0 || amount != fee)
            throw new ArgumentException(
                $"El pago debe ser exactamente S/ {fee:F2}.");

        if (hasConfirmedPayment)
            throw new ArgumentException(
                "La reprogramación ya tiene un pago registrado.");

        AppointmentRules.ValidatePaymentExpiration(
            expiresAt, now);
    }

    public static void ValidateCancellation(string status)
    {
        if (status != "PENDING_PAYMENT" &&
            status != "CONFIRMED")
        {
            throw new ArgumentException(
                "Solo se pueden cancelar citas pendientes de pago o confirmadas.");
        }
    }
    
    public static void ValidateNoShow(
        string status,
        DateTime endAt,
        DateTime now)
    {
        if (status != "CONFIRMED")
            throw new ArgumentException(
                "Solo se puede registrar la inasistencia de una cita confirmada.");

        if (now < endAt)
            throw new ArgumentException(
                "Debe finalizar el horario reservado para registrar la inasistencia.");
    }

    public static void ValidateDepositForfeiture(
        decimal amount,
        bool alreadyForfeited,
        decimal appliedAmount)
    {
        if (amount <= 0)
            throw new ArgumentException(
                "El adelanto debe ser mayor que cero.");

        if (alreadyForfeited)
            throw new ArgumentException(
                "El adelanto ya fue retenido.");

        if (appliedAmount > 0)
            throw new ArgumentException(
                "No se puede retener un adelanto aplicado a una venta.");
    }
}