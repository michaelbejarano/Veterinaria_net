
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Validators.Employee;
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Employee.Commands;

public class UpdateEmployeeCommand : IRequest<bool>
{
    public long Id { get; set; }

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

public class UpdateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    EmployeeValidator employeeValidator,
    EmployeePermissionValidator permissionValidator
) : IRequestHandler<UpdateEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // ========================================
        // 1. VALIDAR PERMISOS
        // ========================================

        await permissionValidator.ValidateManageAsync();

        // ========================================
        // 2. BUSCAR EMPLEADO POR ID
        // ========================================

        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.Id);

        if (employee is null)
        {
            return false;
        }

        // ========================================
        // 3. VALIDAR NUEVOS DATOS
        // ========================================

        await employeeValidator.ValidateAsync(
            request.UserId,
            request.FirstNames,
            request.LastNames,
            request.Type,
            request.MonthlySalary,
            request.DocumentNumber,
            request.LicenseNumber,
            request.Id
        );

        // ========================================
        // 4. ACTUALIZAR DATOS DEL EMPLEADO
        // ========================================

        employee.UserId = request.UserId;
        employee.FirstNames = request.FirstNames.Trim();
        employee.LastNames = request.LastNames.Trim();
        employee.DocumentNumber = request.DocumentNumber;
        employee.Phone = request.Phone;
        employee.Type = request.Type;
        employee.LicenseNumber = request.LicenseNumber;
        employee.Specialty = request.Specialty;
        employee.MonthlySalary = request.MonthlySalary;

        // ========================================
        // 5. ACTUALIZAR EN EL REPOSITORIO
        // ========================================

        unitOfWork
            .Repository<EmployeeEntity>()
            .Update(employee);

        // ========================================
        // 6. GUARDAR CAMBIOS EN POSTGRESQL
        // ========================================

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
