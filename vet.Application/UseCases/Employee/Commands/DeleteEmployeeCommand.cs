using MediatR;
using vet.Application.Services;
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
    UserAuthorizationService authorizationService
) : IRequestHandler<DeleteEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar autorización
        await authorizationService.GetCurrentRoleAsync();

        // 2. Buscar empleado
        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.Id);

        if (employee is null)
            return false;

        // 3. Evitar desactivación repetida
        if (!employee.IsActive)
            return true;

        // 4. Desactivación lógica
        employee.IsActive = false;

        unitOfWork.Repository<EmployeeEntity>()
            .Update(employee);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
