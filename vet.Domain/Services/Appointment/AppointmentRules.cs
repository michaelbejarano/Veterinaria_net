
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

        if (startAt.Date != endAt.Date)
            throw new ArgumentException(
                "La cita debe comenzar y terminar el mismo día.");
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
        IEnumerable<ScheduleBlock> blocks)
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
}
