
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
using EmployeeEntity = vet.Domain.Entities.Employee;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Commands;

public class CreateEmployeeScheduleCommand : IRequest<long>
{
    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Range(1, 7)]
    public short Weekday { get; set; }

    public TimeOnly StartAt { get; set; }

    public TimeOnly EndAt { get; set; }
}

public class CreateEmployeeScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<CreateEmployeeScheduleCommand, long>
{
    public async Task<long> Handle(
        CreateEmployeeScheduleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar autorización
        await authorizationService.GetCurrentRoleAsync();

        // 2. Aplicar reglas de negocio
        EmployeeScheduleRules.Validate(
            request.Weekday,
            request.StartAt,
            request.EndAt);

        // 3. Verificar empleado
        var employee = await unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null)
        {
            throw new ArgumentException(
                "El empleado no existe.");
        }

        if (!employee.IsActive)
        {
            throw new InvalidOperationException(
                "No se pueden asignar horarios a empleados inactivos.");
        }

        // 4. Consultar horarios del mismo día
        var schedules = await unitOfWork
            .Repository<ScheduleEntity>()
            .FindAsync(s =>
                s.EmployeeId == request.EmployeeId &&
                s.Weekday == request.Weekday);

        // 5. Comprobar superposiciones
        var hasOverlap = schedules.Any(s =>
            EmployeeScheduleRules.HasOverlap(
                request.StartAt,
                request.EndAt,
                s.StartAt,
                s.EndAt));

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "El empleado ya tiene un horario que se superpone.");
        }

        // 6. Crear horario
        var schedule = new ScheduleEntity
        {
            EmployeeId = request.EmployeeId,
            Weekday = request.Weekday,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        await unitOfWork
            .Repository<ScheduleEntity>()
            .AddAsync(schedule);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return schedule.Id;
    }
}
