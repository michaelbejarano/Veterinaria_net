using MediatR;
using vet.Application.DTOs.Employee;
using vet.Application.Services;
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Employee.Querys;

public record GetEmployeeByIdQuery(long Id)
    : IRequest<EmployeeDto?>;

public class GetEmployeeByIdQueryHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
{
    public async Task<EmployeeDto?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar permiso para consultar salarios
        var canViewSalary = false;

        try
        {
            var role = await authorizationService.GetCurrentRoleAsync();

            canViewSalary = role is "SUPER_ADMIN" or "ADMIN";
        }
        catch (UnauthorizedAccessException)
        {
            canViewSalary = false;
        }

        // 2. Buscar trabajador
        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.Id);

        if (employee is null)
            return null;

        // 3. Mapear DTO
        return new EmployeeDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            FirstNames = employee.FirstNames,
            LastNames = employee.LastNames,
            DocumentNumber = employee.DocumentNumber,
            Phone = employee.Phone,
            Type = employee.Type,
            LicenseNumber = employee.LicenseNumber,
            Specialty = employee.Specialty,
            MonthlySalary = canViewSalary
                ? employee.MonthlySalary
                : null,
            IsActive = employee.IsActive
        };
    }
}