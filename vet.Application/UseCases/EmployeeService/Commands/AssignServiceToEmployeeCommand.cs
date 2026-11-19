
using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;
using EmployeeEntity = vet.Domain.Entities.Employee;
using ServiceEntity = vet.Domain.Entities.Service;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;

namespace vet.Application.UseCases.EmployeeService.Commands;

public class AssignServiceToEmployeeCommand : IRequest<long>
{
    public long EmployeeId { get; set; }
    public long ServiceId { get; set; }
}

public class AssignServiceToEmployeeCommandHandler
    : IRequestHandler<AssignServiceToEmployeeCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignServiceToEmployeeCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        AssignServiceToEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar trabajador
        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null)
            throw new ArgumentException(
                "El trabajador no existe.");

        if (!employee.IsActive)
            throw new ArgumentException(
                "El trabajador está inactivo.");

        // 2. Verificar servicio
        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.ServiceId);

        if (service is null)
            throw new ArgumentException(
                "El servicio no existe.");

        if (!service.IsActive)
            throw new ArgumentException(
                "El servicio está inactivo.");

        // 3. Validar regla fundamental del dominio
        EmployeeServiceRules.ValidateAssignment(
            employee, service);

        // 4. Comprobar asignación existente
        var existing = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .FindAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.ServiceId == request.ServiceId);

        var assignment = existing.FirstOrDefault();

        if (assignment is not null)
        {
            // Si ya está habilitado, devolver su ID
            if (assignment.IsEnabled)
                return assignment.Id;

            // Reactivar asignación existente
            assignment.IsEnabled = true;

            _unitOfWork
                .Repository<EmployeeServiceEntity>()
                .Update(assignment);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return assignment.Id;
        }

        // 5. Crear asignación
        var employeeService = new EmployeeServiceEntity
        {
            EmployeeId = request.EmployeeId,
            ServiceId = request.ServiceId,
            IsEnabled = true
        };

        await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .AddAsync(employeeService);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return employeeService.Id;
    }
}
