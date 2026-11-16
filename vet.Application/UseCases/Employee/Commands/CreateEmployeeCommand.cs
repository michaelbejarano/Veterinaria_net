
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Validators.Employee;
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Employee.Commands;

public class CreateEmployeeCommand : IRequest<long>
{
    public long? UserId { get; set; }

    [Required]
    public string FirstNames { get; set; } = string.Empty;

    [Required]
    public string LastNames { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }

    public string? Phone { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    public string? LicenseNumber { get; set; }

    public string? Specialty { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlySalary { get; set; }
}

public class CreateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    EmployeeValidator employeeValidator,
    EmployeePermissionValidator permissionValidator
) : IRequestHandler<CreateEmployeeCommand, long>
{
    public async Task<long> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar permisos
        await permissionValidator.ValidateManageAsync();

        // 2. Validar datos del empleado
        await employeeValidator.ValidateAsync(
            request.UserId,
            request.FirstNames,
            request.LastNames,
            request.Type,
            request.MonthlySalary,
            request.DocumentNumber,
            request.LicenseNumber
        );

        // 3. Crear entidad
        var employee = new EmployeeEntity
        {
            UserId = request.UserId,
            FirstNames = request.FirstNames.Trim(),
            LastNames = request.LastNames.Trim(),
            DocumentNumber = request.DocumentNumber,
            Phone = request.Phone,
            Type = request.Type,
            LicenseNumber = request.LicenseNumber,
            Specialty = request.Specialty,
            MonthlySalary = request.MonthlySalary,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Guardar usando repositorio genérico
        await unitOfWork.Repository<EmployeeEntity>()
            .AddAsync(employee);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
