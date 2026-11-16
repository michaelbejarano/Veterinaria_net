using MediatR;
using vet.Application.Validators.Employee;
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Employee.Commands;

public class DeleteEmployeeCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    EmployeePermissionValidator permissionValidator
) : IRequestHandler<DeleteEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar permisos
        await permissionValidator.ValidateManageAsync();

        // 2. Buscar empleado
        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.Id);

        if (employee is null)
        {
            return false;
        }

        // 3. Desactivar empleado
        if (!employee.IsActive)
        {
            return true;
        }

        employee.IsActive = false;

        // 4. Actualizar usando repositorio generico
        unitOfWork.Repository<EmployeeEntity>()
            .Update(employee);

        // 5. Guardar cambios
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}