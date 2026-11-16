
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;
using UserEntity = vet.Domain.Entities.User;

namespace vet.Application.Validators.Employee;

public class EmployeeValidator(IUnitOfWork unitOfWork)
{
    public async Task ValidateAsync(
        long? userId,
        string firstNames,
        string lastNames,
        string type,
        decimal monthlySalary,
        string? documentNumber,
        string? licenseNumber,
        long? currentEmployeeId = null)
    {
        // ========================================
        // 1. VALIDAR NOMBRES Y APELLIDOS
        // ========================================

        if (string.IsNullOrWhiteSpace(firstNames) ||
            string.IsNullOrWhiteSpace(lastNames))
        {
            throw new ArgumentException(
                "Los nombres y apellidos son obligatorios.");
        }

        // ========================================
        // 2. VALIDAR TIPO DE EMPLEADO
        // ========================================

        var allowedTypes = new[]
        {
            "VETERINARIAN",
            "GROOMING",
            "RECEPTION",
            "ADMIN",
            "OTHER"
        };

        if (string.IsNullOrWhiteSpace(type) ||
            !allowedTypes.Contains(type))
        {
            throw new ArgumentException(
                "El tipo de empleado no es válido.");
        }

        // ========================================
        // 3. VALIDAR SALARIO
        // ========================================

        if (monthlySalary < 0)
        {
            throw new ArgumentException(
                "El salario no puede ser negativo.");
        }

        // ========================================
        // 4. VALIDAR USUARIO VINCULADO
        // ========================================

        if (userId.HasValue)
        {
            var user = await unitOfWork
                .Repository<UserEntity>()
                .GetByIdAsync(userId.Value);

            if (user is null)
            {
                throw new ArgumentException(
                    "El usuario indicado no existe.");
            }

            var linkedEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.UserId == userId.Value &&
                    (!currentEmployeeId.HasValue ||
                     e.Id != currentEmployeeId.Value));

            if (linkedEmployees.Any())
            {
                throw new InvalidOperationException(
                    "El usuario ya está vinculado a otro empleado.");
            }
        }

        // ========================================
        // 5. VALIDAR DOCUMENTO ÚNICO
        // ========================================

        if (!string.IsNullOrWhiteSpace(documentNumber))
        {
            var existingEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.DocumentNumber == documentNumber &&
                    (!currentEmployeeId.HasValue ||
                     e.Id != currentEmployeeId.Value));

            if (existingEmployees.Any())
            {
                throw new InvalidOperationException(
                    "El documento ya está registrado.");
            }
        }

        // ========================================
        // 6. VALIDAR COLEGIATURA ÚNICA
        // ========================================

        if (!string.IsNullOrWhiteSpace(licenseNumber))
        {
            var existingEmployees = await unitOfWork
                .Repository<EmployeeEntity>()
                .FindAsync(e =>
                    e.LicenseNumber == licenseNumber &&
                    (!currentEmployeeId.HasValue ||
                     e.Id != currentEmployeeId.Value));

            if (existingEmployees.Any())
            {
                throw new InvalidOperationException(
                    "El número de colegiatura ya está registrado.");
            }
        }
    }
}
