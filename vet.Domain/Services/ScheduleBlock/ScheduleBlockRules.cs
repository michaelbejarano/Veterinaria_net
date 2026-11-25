using vet.Domain.Entities;

namespace vet.Domain.Services.ScheduleBlock;

public static class ScheduleBlockRules
{
    public static void ValidateDates(
        DateTime startAt,
        DateTime endAt)
    {
        if (startAt >= endAt)
            throw new ArgumentException(
                "La fecha de inicio debe ser anterior a la fecha de fin.");
    }

    public static void ValidateBlockConflicts(
        DateTime startAt,
        DateTime endAt,
        IEnumerable<Entities.ScheduleBlock> blocks)
    {
        var hasConflict = blocks.Any(b =>
            AppointmentRules.Overlaps(
                startAt,
                endAt,
                b.StartAt,
                b.EndAt));

        if (hasConflict)
            throw new ArgumentException(
                "El trabajador ya tiene un bloqueo en ese horario.");
    }

    public static void ValidateAppointmentConflicts(
        DateTime startAt,
        DateTime endAt,
        IEnumerable<Appointment> appointments)
    {
        var hasConflict = appointments.Any(a =>
            (a.Status == "PENDING_PAYMENT" ||
             a.Status == "CONFIRMED" ||
             a.Status == "IN_PROGRESS") &&
            AppointmentRules.Overlaps(
                startAt,
                endAt,
                a.StartAt,
                a.EndAt));

        if (hasConflict)
            throw new ArgumentException(
                "No se puede bloquear un horario que tiene citas reservadas.");
    }
}