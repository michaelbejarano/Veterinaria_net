using MediatR;
using vet.Domain.Ports;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;
using EmployeeEntity = vet.Domain.Entities.Employee;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.EmployeeService.Querys;

public record EmployeeServiceDto(
    long Id,
    long EmployeeId,
    string EmployeeName,
    long ServiceId,
    string ServiceName,
    bool IsEnabled
);

public class GetAllEmployeeServicesQuery
    : IRequest<List<EmployeeServiceDto>>
{
}

public class GetAllEmployeeServicesQueryHandler
    : IRequestHandler<GetAllEmployeeServicesQuery, List<EmployeeServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllEmployeeServicesQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EmployeeServiceDto>> Handle(
        GetAllEmployeeServicesQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .GetAllAsync();

        var employees = (await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetAllAsync()).ToDictionary(x => x.Id);

        var services = (await _unitOfWork
            .Repository<ServiceEntity>()
            .GetAllAsync()).ToDictionary(x => x.Id);

        return assignments.Select(x => new EmployeeServiceDto(
            x.Id,
            x.EmployeeId,
            employees.TryGetValue(x.EmployeeId, out var employee)
                ? $"{employee.FirstNames} {employee.LastNames}"
                : string.Empty,
            x.ServiceId,
            services.TryGetValue(x.ServiceId, out var service)
                ? service.Name
                : string.Empty,
            x.IsEnabled
        )).ToList();
    }
}