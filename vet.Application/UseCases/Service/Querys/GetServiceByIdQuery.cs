using MediatR;
using vet.Domain.Ports;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Service.Querys;

public class GetServiceByIdQuery : IRequest<ServiceDto?>
{
    public long Id { get; set; }
}

public class GetServiceByIdQueryHandler
    : IRequestHandler<GetServiceByIdQuery, ServiceDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServiceByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceDto?> Handle(
        GetServiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.Id);

        if (service is null)
            return null;

        return new ServiceDto(
            service.Id,
            service.Name,
            service.Description,
            service.BasePrice,
            service.DurationMinutes,
            service.IsClinical,
            service.IsActive
        );
    }
}