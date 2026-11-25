namespace vet.Domain.Services.consultation;

public static class ConsultationRules
{
    public static void ValidateCreation(
        string appointmentStatus,
        string visitStatus,
        bool alreadyExists)
    {
        if (appointmentStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La cita debe estar en curso.");

        if (visitStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La atención debe estar en curso.");

        if (alreadyExists)
            throw new ArgumentException(
                "La atención ya tiene una consulta médica.");
    }

    public static void ValidateWeight(decimal? weightKg)
    {
        if (weightKg.HasValue && weightKg.Value <= 0)
            throw new ArgumentException(
                "El peso debe ser mayor que cero.");
    }

    public static void ValidateClinicalService(
        bool hasClinicalService)
    {
        if (!hasClinicalService)
            throw new ArgumentException(
                "La atención no tiene servicios clínicos.");
    }
    
    public static void ValidateUpdate(
        string appointmentStatus,
        string visitStatus)
    {
        if (appointmentStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "La cita debe estar en curso.");

        if (visitStatus != "IN_PROGRESS")
            throw new ArgumentException(
                "No se puede modificar una atención finalizada.");
    }
    
    
}