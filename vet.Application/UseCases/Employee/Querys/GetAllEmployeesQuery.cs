using MediatR;
using vet.Application.DTOs.Employee;
using vet.Application.Services;
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Employee.Querys;

public record GetAllEmployeesQuery(bool? IsActive = true)
    : IRequest<IEnumerable<EmployeeDto>>;

public class GetAllEmployeesQueryHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeDto>>
{
    public async Task<IEnumerable<EmployeeDto>> Handle(
        GetAllEmployeesQuery request,
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
            // Recepción puede consultar trabajadores,
            // pero no sus salarios.
            canViewSalary = false;
        }

        // 2. Consultar trabajadores
        var employees = request.IsActive.HasValue
            ? await unitOfWork.Repository<EmployeeEntity>()
                .FindAsync(e => e.IsActive == request.IsActive.Value)
            : await unitOfWork.Repository<EmployeeEntity>()
                .GetAllAsync();

        // 3. Mapear DTO
        return employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            UserId = e.UserId,
            FirstNames = e.FirstNames,
            LastNames = e.LastNames,
            DocumentNumber = e.DocumentNumber,
            Phone = e.Phone,
            Type = e.Type,
            LicenseNumber = e.LicenseNumber,
            Specialty = e.Specialty,
            MonthlySalary = canViewSalary
                ? e.MonthlySalary
                : null,
            IsActive = e.IsActive
        });
    }
}