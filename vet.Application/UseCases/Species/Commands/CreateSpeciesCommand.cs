using MediatR;
using vet.Domain.Ports;
using SpeciesEntity = vet.Domain.Entities.Species;

namespace vet.Application.UseCases.Species.Commands;

public class CreateSpeciesCommand : IRequest<long>
{
    public string Name { get; set; } = string.Empty;
}

public class CreateSpeciesCommandHandler : IRequestHandler<CreateSpeciesCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpeciesCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateSpeciesCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
            throw new ArgumentException(
                "La especie debe tener entre 1 y 80 caracteres.");

        var repo = _unitOfWork.Repository<SpeciesEntity>();

        var existing = await repo.FindAsync(x =>
            x.Name.ToLower() == name.ToLower());

        if (existing.Any())
            throw new ArgumentException(
                "Esta especie ya está registrada.");

        var species = new SpeciesEntity
        {
            Name = name
        };

        await repo.AddAsync(species);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return species.Id;
    }
}