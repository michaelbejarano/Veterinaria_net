
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
using UserEntity = vet.Domain.Entities.User;
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
    UserAuthorizationService authorizationService
) : IRequestHandler<CreateEmployeeCommand, long>
{
    public async Task<long> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar autorización
        await authorizationService.GetCurrentRoleAsync();

        // 2. Aplicar reglas de negocio del dominio
        EmployeeRules.Validate(
            request.FirstNames,
            request.LastNames,
            request.Type,
            request.MonthlySalary,
            request.DocumentNumber,
            request.Phone);

        // 3. Verificar usuario vinculado
        if (request.UserId.HasValue)
        {
            var user = await unitOfWork
                .Repository<UserEntity>()
                .GetByIdAsync(request.UserId.Value);

            if (user is null)
                throw new ArgumentException(
                    "El usuario indicado no existe.");

            var linkedEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e => e.UserId == request.UserId.Value);

            if (linkedEmployees.Any())
                throw new InvalidOperationException(
                    "El usuario ya está vinculado a otro empleado.");
        }

        // 4. Verificar documento único
        if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            var existing = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.DocumentNumber == request.DocumentNumber);

            if (existing.Any())
                throw new InvalidOperationException(
                    "El documento ya está registrado.");
        }

        // 5. Verificar colegiatura única
        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var existing = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.LicenseNumber == request.LicenseNumber);

            if (existing.Any())
                throw new InvalidOperationException(
                    "El número de colegiatura ya está registrado.");
        }

        // 6. Crear empleado
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

        await unitOfWork.Repository<EmployeeEntity>()
            .AddAsync(employee);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}

