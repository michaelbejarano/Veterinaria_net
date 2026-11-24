using MediatR;
using vet.Application.DTOs.Pet;
using vet.Domain.Ports;
using PetEntity = vet.Domain.Entities.Pet;
using OwnerEntity = vet.Domain.Entities.Owner;
using SpeciesEntity = vet.Domain.Entities.Species;
using BreedEntity = vet.Domain.Entities.Breed;

namespace vet.Application.UseCases.Pet.Querys;

public class GetPetByIdQuery : IRequest<PetDto?>
{
    public long Id { get; set; }
}

public class GetPetByIdQueryHandler
    : IRequestHandler<GetPetByIdQuery, PetDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPetByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PetDto?> Handle(
        GetPetByIdQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await _unitOfWork
            .Repository<PetEntity>()
            .GetByIdAsync(request.Id);

        if (pet is null)
            return null;

        var owner = await _unitOfWork
            .Repository<OwnerEntity>()
            .GetByIdAsync(pet.OwnerId);

        var species = await _unitOfWork
            .Repository<SpeciesEntity>()
            .GetByIdAsync(pet.SpeciesId);

        BreedEntity? breed = null;

        if (pet.BreedId.HasValue)
        {
            breed = await _unitOfWork
                .Repository<BreedEntity>()
                .GetByIdAsync(pet.BreedId.Value);
        }

        return new PetDto
        {
            Id = pet.Id,
            OwnerId = pet.OwnerId,
            OwnerName = owner?.FirstNames ?? string.Empty,
            SpeciesId = pet.SpeciesId,
            SpeciesName = species?.Name ?? string.Empty,
            BreedId = pet.BreedId,
            BreedName = breed?.Name,
            Name = pet.Name,
            Sexo = pet.Sexo,
            BirthDate = pet.BirthDate,
            Notes = pet.Notes,
            IsActive = pet.IsActive
        };
    }
}