using MediatR;
using vet.Domain.Ports;
using BreedEntity = vet.Domain.Entities.Breed;
using SpeciesEntity = vet.Domain.Entities.Species;

namespace vet.Application.UseCases.Breed.Commands;

public class CreateBreedCommand : IRequest<long>
{
    public long SpeciesId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateBreedCommandHandler : IRequestHandler<CreateBreedCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBreedCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateBreedCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new ArgumentException(
                "La raza debe tener entre 1 y 100 caracteres.");

        var species = await _unitOfWork
            .Repository<SpeciesEntity>()
            .GetByIdAsync(request.SpeciesId);

        if (species is null)
            throw new KeyNotFoundException(
                "La especie seleccionada no existe.");

        var repo = _unitOfWork.Repository<BreedEntity>();

        var existing = await repo.FindAsync(x =>
            x.SpeciesId == request.SpeciesId &&
            x.Name.ToLower() == name.ToLower());

        if (existing.Any())
            throw new ArgumentException(
                "Esta raza ya existe para la especie seleccionada.");

        var breed = new BreedEntity
        {
            SpeciesId = request.SpeciesId,
            Name = name
        };

        await repo.AddAsync(breed);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return breed.Id;
    }
}