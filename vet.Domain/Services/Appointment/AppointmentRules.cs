
using vet.Domain.Entities;

namespace vet.Domain.Services;

public static class AppointmentRules
{
    public const decimal DepositPercent = 30m;

    public static void ValidateDates(
        DateTime startAt,
        DateTime endAt,
        DateTime now)
    {
        if (startAt >= endAt)
            throw new ArgumentException(
                "La hora de inicio debe ser anterior a la hora de fin.");

        if (startAt <= now)
            throw new ArgumentException(
                "La cita debe programarse para una fecha futura.");

       
    }
    
    public static bool Overlaps(
        DateTime startAt,
        DateTime endAt,
        DateTime otherStart,
        DateTime otherEnd)
    {
        return startAt < otherEnd &&
               endAt > otherStart;
    }


    public static void ValidateWorkingHours(
        DateTime startAt,
        DateTime endAt,
        IEnumerable<EmployeeSchedule> schedules,
        short weekday)
    {
        if (startAt.Date != endAt.Date)
            throw new ArgumentException(
                "La cita debe comenzar y terminar el mismo día.");

        var startTime = TimeOnly.FromDateTime(startAt);
        var endTime = TimeOnly.FromDateTime(endAt);

        var isWithinSchedule = schedules.Any(s =>
            s.Weekday == weekday &&
            s.StartAt <= startTime &&
            s.EndAt >= endTime);

        if (!isWithinSchedule)
            throw new ArgumentException(
                "La cita está fuera del horario laboral del trabajador.");
    }

    public static void ValidateScheduleBlocks(
        DateTime startAt,
        DateTime endAt,
        IEnumerable<Entities.ScheduleBlock> blocks)
    {
        var hasConflict = blocks.Any(b =>
            startAt < b.EndAt &&
            endAt > b.StartAt);

        if (hasConflict)
            throw new ArgumentException(
                "El trabajador tiene un bloqueo en ese horario.");
    }

    public static void ValidateAppointmentConflicts(
        DateTime startAt,
        DateTime endAt,
        IEnumerable<Appointment> appointments)
    {
        var hasConflict = appointments.Any(a =>
            a.Status != "CANCELLED" &&
            a.Status != "EXPIRED" &&
            startAt < a.EndAt &&
            endAt > a.StartAt);

        if (hasConflict)
            throw new ArgumentException(
                "El trabajador ya tiene una cita en ese horario.");
    }

    public static decimal CalculateRequiredDeposit(
        decimal reservedPrice)
    {
        if (reservedPrice <= 0)
            throw new ArgumentException(
                "El precio de la reserva debe ser mayor que cero.");

        return Math.Round(
            reservedPrice * DepositPercent / 100m,
            2,
            MidpointRounding.AwayFromZero);
    }
    
    public static void ValidateDuration(
        DateTime startAt,
        DateTime endAt,
        int requiredMinutes)
    {
        if (requiredMinutes <= 0)
            throw new ArgumentException(
                "La duración de los servicios debe ser mayor que cero.");

        var reservedMinutes = (endAt - startAt).TotalMinutes;

        if (reservedMinutes != requiredMinutes)
            throw new ArgumentException(
                $"La cita debe durar exactamente {requiredMinutes} minutos.");
    }
    
    
    public static void ValidateSlotAlignment(
        DateTime localStart,
        DateTime localEnd,
        IEnumerable<EmployeeSchedule> schedules,
        short weekday,
        int requiredMinutes)
    {
        if (requiredMinutes <= 0)
            throw new ArgumentException(
                "La duración debe ser mayor que cero.");

        var validSlot = schedules
            .Where(s => s.Weekday == weekday)
            .Any(schedule =>
            {
                var shiftStart = localStart.Date
                    .Add(schedule.StartAt.ToTimeSpan());

                var shiftEnd = localStart.Date
                    .Add(schedule.EndAt.ToTimeSpan());

                if (localStart < shiftStart ||
                    localEnd > shiftEnd)
                    return false;

                var elapsed = localStart - shiftStart;

                return elapsed.Ticks %
                    TimeSpan.FromMinutes(requiredMinutes).Ticks == 0;
            });

        if (!validSlot)
            throw new ArgumentException(
                "La hora seleccionada no corresponde a un bloque disponible.");
    }
    

    public static void ValidateCancellation(string status)
    {
        if (status != "PENDING_PAYMENT")
        {
            throw new ArgumentException(
                "Por ahora solo se pueden cancelar citas pendientes de pago.");
        }
    }

    public static void ValidateDepositPayment(
        string appointmentStatus,
        decimal requiredDeposit,
        decimal amount,
        bool hasConfirmedPayment)
    {
        if (appointmentStatus != "PENDING_PAYMENT")
            throw new ArgumentException(
                "Solo se pueden pagar adelantos de citas pendientes de pago.");

        if (requiredDeposit <= 0)
            throw new ArgumentException(
                "La cita no requiere un adelanto válido.");

        if (amount != requiredDeposit)
            throw new ArgumentException(
                $"El adelanto debe ser exactamente S/ {requiredDeposit:F2}.");

        if (hasConfirmedPayment)
            throw new ArgumentException(
                "Esta cita ya tiene un adelanto registrado.");
    }

    public static void ValidatePaymentMethod(string method)
    {
        string[] allowedMethods =
        {
            "CASH",
            "YAPE",
            "PLIN",
            "CARD",
            "BANK_TRANSFER"
        };

        if (!allowedMethods.Contains(method))
            throw new ArgumentException(
                "El método de pago no está permitido.");
    }

    public static void ValidateOpenCashSession(string status)
    {
        if (status != "OPEN")
            throw new ArgumentException(
                "La sesión de caja debe estar abierta.");
    }
    public static DateTime CalculatePaymentExpiration(
        DateTime createdAt)
    {
        if (createdAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "La fecha de creación debe estar en UTC.");

        return createdAt.AddMinutes(15);
    }
    //valdiamos el vencimiento 15 m
    public static void ValidatePaymentExpiration(
        DateTime? paymentExpiresAt,
        DateTime currentUtc)
    {
        if (!paymentExpiresAt.HasValue)
            throw new ArgumentException(
                "La cita no tiene una fecha límite de pago configurada.");

        if (currentUtc >= paymentExpiresAt.Value)
            throw new ArgumentException(
                "El plazo de 15 minutos para pagar ha vencido.");
    }

    public static bool ShouldExpire(
        string status,
        DateTime? paymentExpiresAt,
        DateTime currentUtc)
    {
        return status == "PENDING_PAYMENT"
               && paymentExpiresAt.HasValue
               && currentUtc >= paymentExpiresAt.Value;
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
