using MediatR;
using vet.Domain.Ports;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.EmployeeService.Querys;

public record ServiceEmployeeDto(
    long EmployeeId,
    string EmployeeName,
    string EmployeeType
);

public class GetEmployeesByServiceQuery
    : IRequest<List<ServiceEmployeeDto>>
{
    public long ServiceId { get; set; }
}

public class GetEmployeesByServiceQueryHandler
    : IRequestHandler<GetEmployeesByServiceQuery, List<ServiceEmployeeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEmployeesByServiceQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceEmployeeDto>> Handle(
        GetEmployeesByServiceQuery request,
        CancellationToken cancellationToken)
    {
        // Obtener asignaciones habilitadas
        var assignments = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .FindAsync(x =>
                x.ServiceId == request.ServiceId &&
                x.IsEnabled);

        // Obtener empleados activos
        var employees = await _unitOfWork
            .Repository<EmployeeEntity>()
            .FindAsync(x => x.IsActive);

        var employeeDictionary = employees
            .ToDictionary(x => x.Id);

        // Construir respuesta
        var result = assignments
            .Where(x => employeeDictionary.ContainsKey(x.EmployeeId))
            .Select(x =>
            {
                var employee = employeeDictionary[x.EmployeeId];

                return new ServiceEmployeeDto(
                    employee.Id,
                    $"{employee.FirstNames} {employee.LastNames}",
                    employee.Type
                );
            })
            .ToList();

        return result;
    }
}