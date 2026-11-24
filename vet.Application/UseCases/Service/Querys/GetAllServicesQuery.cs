using MediatR;
using vet.Domain.Ports;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Service.Querys;

public record ServiceDto(
    long Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int DurationMinutes,
    bool IsClinical,
    bool IsActive
);

public class GetAllServicesQuery : IRequest<List<ServiceDto>>
{
}

public class GetAllServicesQueryHandler
    : IRequestHandler<GetAllServicesQuery, List<ServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllServicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceDto>> Handle(
        GetAllServicesQuery request,
        CancellationToken cancellationToken)
    {
        var services = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetAllAsync();

        return services
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.BasePrice,
                s.DurationMinutes,
                s.IsClinical,
                s.IsActive
            ))
            .ToList();
    }
}