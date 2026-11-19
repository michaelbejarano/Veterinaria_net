using MediatR;
using vet.Application.DTOs.Pet;
using vet.Domain.Ports;
using PetEntity = vet.Domain.Entities.Pet;

namespace vet.Application.UseCases.Pet.Querys;

public class GetAllPetsQuery : IRequest<List<PetDto>>
{
}

public class GetAllPetsQueryHandler
    : IRequestHandler<GetAllPetsQuery, List<PetDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllPetsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PetDto>> Handle(
        GetAllPetsQuery request,
        CancellationToken cancellationToken)
    {
        var pets = await _unitOfWork
            .Repository<PetEntity>()
            .GetAllAsync();

        var owners = (await _unitOfWork
            .Repository<vet.Domain.Entities.Owner>()
            .GetAllAsync()).ToDictionary(x => x.Id);

        var species = (await _unitOfWork
            .Repository<vet.Domain.Entities.Species>()
            .GetAllAsync()).ToDictionary(x => x.Id);

        var breeds = (await _unitOfWork
            .Repository<vet.Domain.Entities.Breed>()
            .GetAllAsync()).ToDictionary(x => x.Id);

        return pets.Select(p => new PetDto
        {
            Id = p.Id,
            OwnerId = p.OwnerId,
            OwnerName = owners.TryGetValue(
                p.OwnerId, out var owner)
                ? owner.FirstNames : string.Empty,

            SpeciesId = p.SpeciesId,
            SpeciesName = species.TryGetValue(
                p.SpeciesId, out var specie)
                ? specie.Name : string.Empty,

            BreedId = p.BreedId,
            BreedName = p.BreedId.HasValue &&
                        breeds.TryGetValue(
                            p.BreedId.Value, out var breed)
                ? breed.Name : null,

            Name = p.Name,
            Sexo = p.Sexo,
            BirthDate = p.BirthDate,
            Notes = p.Notes,
            IsActive = p.IsActive
        }).ToList();
    }
}