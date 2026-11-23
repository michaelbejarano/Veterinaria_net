namespace vet.Domain.Services;

public static class ProductRules
{
    public static void Validate(
        string name,
        string type,
        string unit,
        decimal salePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "El nombre del producto es obligatorio.");

        if (type != "PRODUCT" &&
            type != "MEDICINE" &&
            type != "VACCINE" &&
            type != "SUPPLY")
            throw new ArgumentException(
                "Tipo de producto inválido.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException(
                "La unidad de medida es obligatoria.");

        if (salePrice < 0)
            throw new ArgumentException(
                "El precio no puede ser negativo.");
    }
}