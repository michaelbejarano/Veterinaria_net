namespace vet.Domain.Services.Treatment;

public static class TreatmentRules
{
    public static void ValidateDescription(
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "La descripción del tratamiento es obligatoria.");
    }
}