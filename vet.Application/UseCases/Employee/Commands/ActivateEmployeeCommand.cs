using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.Employee.Commands;

public class ActivateEmployeeCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class ActivateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<ActivateEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        ActivateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar autorización
        await authorizationService.GetCurrentRoleAsync();

        // 2. Buscar trabajador
        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.Id);

        if (employee is null)
            return false;

        // 3. Verificar si ya está activo
        if (employee.IsActive)
            return true;

        // 4. Reactivar trabajador
        employee.IsActive = true;

        // 5. Actualizar repositorio
        unitOfWork.Repository<EmployeeEntity>()
            .Update(employee);

        // 6. Guardar cambios
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}