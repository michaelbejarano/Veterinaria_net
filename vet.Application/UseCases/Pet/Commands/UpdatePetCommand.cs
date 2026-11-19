
using MediatR;
using vet.Domain.Ports;
using PetEntity = vet.Domain.Entities.Pet;
using OwnerEntity = vet.Domain.Entities.Owner;
using SpeciesEntity = vet.Domain.Entities.Species;
using BreedEntity = vet.Domain.Entities.Breed;

namespace vet.Application.UseCases.Pet.Commands;

public class UpdatePetCommand : IRequest<bool>
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public long SpeciesId { get; set; }
    public long? BreedId { get; set; }
    public string Name { get; set; } = string.Empty;
    public char? Sexo { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePetCommandHandler
    : IRequestHandler<UpdatePetCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePetCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UpdatePetCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _unitOfWork
            .Repository<PetEntity>()
            .GetByIdAsync(request.Id);

        if (pet is null)
            return false;

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 120)
            throw new ArgumentException(
                "El nombre debe tener entre 1 y 120 caracteres.");

        if (request.Sexo.HasValue &&
            request.Sexo != 'M' &&
            request.Sexo != 'F')
            throw new ArgumentException(
                "El sexo debe ser M o F.");

        if (request.BirthDate.HasValue &&
            request.BirthDate.Value >
            DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException(
                "La fecha de nacimiento no puede ser futura.");

        var owner = await _unitOfWork
            .Repository<OwnerEntity>()
            .GetByIdAsync(request.OwnerId);

        if (owner is null)
            throw new ArgumentException(
                "El responsable indicado no existe.");

        var species = await _unitOfWork
            .Repository<SpeciesEntity>()
            .GetByIdAsync(request.SpeciesId);

        if (species is null)
            throw new ArgumentException(
                "La especie indicada no existe.");

        if (request.BreedId.HasValue)
        {
            var breed = await _unitOfWork
                .Repository<BreedEntity>()
                .GetByIdAsync(request.BreedId.Value);

            if (breed is null ||
                breed.SpeciesId != request.SpeciesId)
                throw new ArgumentException(
                    "La raza no existe o no pertenece a la especie.");
        }

        pet.OwnerId = request.OwnerId;
        pet.SpeciesId = request.SpeciesId;
        pet.BreedId = request.BreedId;
        pet.Name = request.Name.Trim();
        pet.Sexo = request.Sexo;
        pet.BirthDate = request.BirthDate;
        pet.Notes = request.Notes?.Trim();

        _unitOfWork.Repository<PetEntity>().Update(pet);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
