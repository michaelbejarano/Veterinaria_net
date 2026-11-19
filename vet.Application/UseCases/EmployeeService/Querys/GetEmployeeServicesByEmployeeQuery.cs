using MediatR;
using vet.Domain.Ports;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.EmployeeService.Querys;

public record AssignedServiceDto(
    long Id,
    long ServiceId,
    string ServiceName,
    bool IsEnabled
);

public class GetEmployeeServicesByEmployeeQuery
    : IRequest<List<AssignedServiceDto>>
{
    public long EmployeeId { get; set; }
}

public class GetEmployeeServicesByEmployeeQueryHandler
    : IRequestHandler<GetEmployeeServicesByEmployeeQuery, List<AssignedServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEmployeeServicesByEmployeeQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AssignedServiceDto>> Handle(
        GetEmployeeServicesByEmployeeQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .FindAsync(x => x.EmployeeId == request.EmployeeId);

        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .GetAllAsync()).ToDictionary(x => x.Id);

        return assignments.Select(x => new AssignedServiceDto(
            x.Id,
            x.ServiceId,
            services.TryGetValue(x.ServiceId, out var service)
                ? service.Name
                : string.Empty,
            x.IsEnabled
        )).ToList();
    }
}