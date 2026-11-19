using MediatR;
using vet.Domain.Ports;
using SpeciesEntity = vet.Domain.Entities.Species;

namespace vet.Application.UseCases.Species.Querys;

public record SpeciesDto(long Id, string Name);

public class GetAllSpeciesQuery : IRequest<List<SpeciesDto>>
{
}

public class GetAllSpeciesQueryHandler
    : IRequestHandler<GetAllSpeciesQuery, List<SpeciesDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllSpeciesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SpeciesDto>> Handle(
        GetAllSpeciesQuery request,
        CancellationToken cancellationToken)
    {
        var species = await _unitOfWork
            .Repository<SpeciesEntity>()
            .GetAllAsync();

        return species
            .OrderBy(x => x.Name)
            .Select(x => new SpeciesDto(x.Id, x.Name))
            .ToList();
    }
}