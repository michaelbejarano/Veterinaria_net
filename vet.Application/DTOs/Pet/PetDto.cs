namespace vet.Application.DTOs.Pet;

public class PetDto
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;

    public long SpeciesId { get; set; }
    public string SpeciesName { get; set; } = string.Empty;

    public long? BreedId { get; set; }
    public string? BreedName { get; set; }

    public string Name { get; set; } = string.Empty;
    public char? Sexo { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}