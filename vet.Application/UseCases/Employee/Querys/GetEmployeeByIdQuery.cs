using MediatR;
using vet.Application.DTOs.Employee;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Employee.Querys;

public record GetEmployeeByIdQuery(long Id)
    : IRequest<EmployeeDto?>;

public class GetEmployeeByIdQueryHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
{
    public async Task<EmployeeDto?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork
            .Repository<vet.Domain.Entities.Employee>()
            .GetByIdAsync(request.Id);

        if (employee is null)
            return null;

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
            IsActive = employee.IsActive
        };
    }
}