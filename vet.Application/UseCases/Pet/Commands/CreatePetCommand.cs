
using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Pet.Commands;

public class CreatePetCommand : IRequest<long>
{
    public long OwnerId { get; set; }
    public long SpeciesId { get; set; }
    public long? BreedId { get; set; }
    public string Name { get; set; } = string.Empty;
    public char? Sexo { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Notes { get; set; }
}

public class CreatePetCommandHandler
    : IRequestHandler<CreatePetCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePetCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreatePetCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException(
                "El nombre de la mascota es obligatorio.");

        if (request.Name.Trim().Length > 120)
            throw new ArgumentException(
                "El nombre no puede superar 120 caracteres.");

        if (request.Sexo.HasValue &&
            request.Sexo != 'M' &&
            request.Sexo != 'F')
            throw new ArgumentException(
                "El sexo debe ser M o F.");

        if (request.BirthDate.HasValue &&
            request.BirthDate.Value > DateOnly.FromDateTime(
                DateTime.UtcNow))
            throw new ArgumentException(
                "La fecha de nacimiento no puede ser futura.");

        var owner = await _unitOfWork
            .Repository<Domain.Entities.Owner>()
            .GetByIdAsync(request.OwnerId);

        if (owner is null)
            throw new ArgumentException(
                "El responsable indicado no existe.");

        var species = await _unitOfWork
            .Repository<Domain.Entities.Species>()
            .GetByIdAsync(request.SpeciesId);

        if (species is null)
            throw new ArgumentException(
                "La especie indicada no existe.");

        if (request.BreedId.HasValue)
        {
            var breed = await _unitOfWork
                .Repository<Domain.Entities.Breed>()
                .GetByIdAsync(request.BreedId.Value);

            if (breed is null)
                throw new ArgumentException(
                    "La raza indicada no existe.");

            if (breed.SpeciesId != request.SpeciesId)
                throw new ArgumentException(
                    "La raza no pertenece a la especie seleccionada.");
        }

        var pet = new vet.Domain.Entities.Pet
        {
            OwnerId = request.OwnerId,
            SpeciesId = request.SpeciesId,
            BreedId = request.BreedId,
            Name = request.Name.Trim(),
            Sexo = request.Sexo,
            BirthDate = request.BirthDate,
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        await _unitOfWork.Repository<vet.Domain.Entities.Pet>()
            .AddAsync(pet);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return pet.Id;
    }
}
