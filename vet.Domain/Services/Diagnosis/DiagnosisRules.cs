namespace vet.Domain.Services.Diagnosis;

public static class DiagnosisRules
{
    public static void ValidateDescription(
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "La descripción del diagnóstico es obligatoria.");
    }

    public static void ValidateCreation(bool alreadyExists)
    {
        if (alreadyExists)
            throw new ArgumentException(
                "La consulta ya tiene un diagnóstico registrado.");
    }
}