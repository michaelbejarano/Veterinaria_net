using MediatR;
using vet.Domain.Ports;

using OwnerEntity = vet.Domain.Entities.Owner;
using PetEntity = vet.Domain.Entities.Pet;

namespace vet.Application.UseCases.Owner.Queries;

public class OwnerPetDto
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public long SpeciesId { get; set; }
    public long? BreedId { get; set; }

    public string Name { get; set; } = string.Empty;
    public char? Sexo { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class GetOwnerPetsQuery : IRequest<List<OwnerPetDto>>
{
    public long OwnerId { get; set; }
    public bool? IsActive { get; set; }
}

public class GetOwnerPetsQueryHandler
    : IRequestHandler<GetOwnerPetsQuery, List<OwnerPetDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOwnerPetsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<OwnerPetDto>> Handle(
        GetOwnerPetsQuery request,
        CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork
            .Repository<OwnerEntity>()
            .GetByIdAsync(request.OwnerId);

        if (owner is null)
            throw new KeyNotFoundException(
                "El propietario no existe.");

        var pets = await _unitOfWork
            .Repository<PetEntity>()
            .FindAsync(p =>
                p.OwnerId == request.OwnerId &&
                (!request.IsActive.HasValue ||
                 p.IsActive == request.IsActive.Value));

        return pets
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(p => new OwnerPetDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                SpeciesId = p.SpeciesId,
                BreedId = p.BreedId,
                Name = p.Name,
                Sexo = p.Sexo,
                BirthDate = p.BirthDate,
                Notes = p.Notes,
                IsActive = p.IsActive
            })
            .ToList();
    }
}