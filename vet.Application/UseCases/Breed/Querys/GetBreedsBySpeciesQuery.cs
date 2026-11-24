using MediatR;
using vet.Domain.Ports;
using BreedEntity = vet.Domain.Entities.Breed;

namespace vet.Application.UseCases.Breed.Querys;

public record BreedDto(long Id, long SpeciesId, string Name);

public class GetBreedsBySpeciesQuery : IRequest<List<BreedDto>>
{
    public long SpeciesId { get; set; }
}

public class GetBreedsBySpeciesQueryHandler
    : IRequestHandler<GetBreedsBySpeciesQuery, List<BreedDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBreedsBySpeciesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<BreedDto>> Handle(
        GetBreedsBySpeciesQuery request,
        CancellationToken cancellationToken)
    {
        var breeds = await _unitOfWork
            .Repository<BreedEntity>()
            .FindAsync(x => x.SpeciesId == request.SpeciesId);

        return breeds
            .OrderBy(x => x.Name)
            .Select(x => new BreedDto(x.Id, x.SpeciesId, x.Name))
            .ToList();
    }
}