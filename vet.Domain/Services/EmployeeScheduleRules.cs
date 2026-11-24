namespace vet.Domain.Services;

public static class EmployeeScheduleRules
{
    public static void Validate(
        short weekday,
        TimeOnly startAt,
        TimeOnly endAt)
    {
        // 1 = lunes, 7 = domingo
        if (weekday < 1 || weekday > 7)
        {
            throw new ArgumentException(
                "El día debe estar entre 1 (lunes) y 7 (domingo).");
        }

        if (startAt >= endAt)
        {
            throw new ArgumentException(
                "La hora de inicio debe ser menor que la hora de fin.");
        }
    }

    public static bool HasOverlap(
        TimeOnly startAt,
        TimeOnly endAt,
        TimeOnly existingStartAt,
        TimeOnly existingEndAt)
    {
        return startAt < existingEndAt &&
               endAt > existingStartAt;
    }
}