namespace vet.Domain.Services;

public static class ServiceRules
{
    public static void Validate(
        string name,
        decimal basePrice,
        int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "El nombre del servicio es obligatorio.");

        if (name.Trim().Length > 150)
            throw new ArgumentException(
                "El nombre no puede superar 150 caracteres.");

        if (basePrice < 0)
            throw new ArgumentException(
                "El precio base no puede ser negativo.");

        if (durationMinutes <= 0)
            throw new ArgumentException(
                "La duración debe ser mayor a cero.");
    }
}