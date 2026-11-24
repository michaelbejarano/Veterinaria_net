using MediatR;
using vet.Application.DTOs.Employee;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Employee.Querys;

public record GetAllEmployeesQuery(bool? IsActive = true)
    : IRequest<IEnumerable<EmployeeDto>>;

public class GetAllEmployeesQueryHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeDto>>
{
    public async Task<IEnumerable<EmployeeDto>> Handle(
        GetAllEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var employees = request.IsActive.HasValue
            ? await unitOfWork.Repository<vet.Domain.Entities.Employee>()
                .FindAsync(e => e.IsActive == request.IsActive.Value)
            : await unitOfWork.Repository<vet.Domain.Entities.Employee>()
                .GetAllAsync();

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
            IsActive = e.IsActive
        });
    }
}