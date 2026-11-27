
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
using EmployeeEntity = vet.Domain.Entities.Employee;
using UserEntity = vet.Domain.Entities.User;

namespace vet.Application.UseCases.Employee.Commands;

// ========================================
// COMMAND
// ========================================

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

// ========================================
// HANDLER
// ========================================

public class UpdateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<UpdateEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // ========================================
        // 1. VALIDAR AUTORIZACIÓN
        // ========================================

        await authorizationService.GetCurrentRoleAsync();

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
        // 3. VALIDAR REGLAS DE NEGOCIO
        // ========================================

        EmployeeRules.Validate(
            request.FirstNames,
            request.LastNames,
            request.Type,
            request.MonthlySalary,
            request.DocumentNumber,
            request.Phone);

        // ========================================
        // 4. VALIDAR USUARIO VINCULADO
        // ========================================

        if (request.UserId.HasValue)
        {
            var user = await unitOfWork
                .Repository<UserEntity>()
                .GetByIdAsync(request.UserId.Value);

            if (user is null)
            {
                throw new ArgumentException(
                    "El usuario indicado no existe.");
            }

            var linkedEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.UserId == request.UserId.Value &&
                    e.Id != request.Id);

            if (linkedEmployees.Any())
            {
                throw new InvalidOperationException(
                    "El usuario ya está vinculado a otro empleado.");
            }
        }

        // ========================================
        // 5. VALIDAR DOCUMENTO ÚNICO
        // ========================================

        if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            var existingEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.DocumentNumber == request.DocumentNumber &&
                    e.Id != request.Id);

            if (existingEmployees.Any())
            {
                throw new InvalidOperationException(
                    "El documento ya está registrado.");
            }
        }

        // ========================================
        // 6. VALIDAR COLEGIATURA ÚNICA
        // ========================================

        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var existingEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.LicenseNumber == request.LicenseNumber &&
                    e.Id != request.Id);

            if (existingEmployees.Any())
            {
                throw new InvalidOperationException(
                    "El número de colegiatura ya está registrado.");
            }
        }

        // ========================================
        // 7. ACTUALIZAR DATOS DEL EMPLEADO
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
        // 8. ACTUALIZAR REPOSITORIO
        // ========================================

        unitOfWork
            .Repository<EmployeeEntity>()
            .Update(employee);

        // ========================================
        // 9. GUARDAR CAMBIOS
        // ========================================

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
