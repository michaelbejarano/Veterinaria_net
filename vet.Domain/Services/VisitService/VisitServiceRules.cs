namespace vet.Domain.Services;

public static class VisitServiceRules
{
    public static void ValidateCompletion(
        string visitStatus,
        string serviceStatus)
    {
        if (visitStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La atención debe estar en curso.");

        if (serviceStatus != "PENDING")
            throw new ArgumentException(
                "Solo se pueden completar servicios pendientes.");
    }

    public static void ValidateFinalPrice(decimal finalPrice)
    {
        if (finalPrice < 0)
            throw new ArgumentException(
                "El precio final no puede ser negativo.");
    }

    public static void ValidateCancellation(
        string visitStatus,
        string serviceStatus)
    {
        if (visitStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La atención debe estar en curso.");

        if (serviceStatus != "PENDING")
            throw new ArgumentException(
                "Solo se pueden anular servicios pendientes.");
    }
    public static void ValidateAddition(
        string appointmentStatus,
        string visitStatus)
    {
        if (appointmentStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La cita debe estar en curso.");

        if (visitStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La atención debe estar en curso.");
    }
}