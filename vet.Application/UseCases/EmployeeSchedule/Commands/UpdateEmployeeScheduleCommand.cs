
using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using vet.Domain.Services;
using EmployeeEntity = vet.Domain.Entities.Employee;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Commands;

public class UpdateEmployeeScheduleCommand : IRequest<bool>
{
    // Se asigna desde la URL
    public long Id { get; set; }

    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Range(1, 7)]
    public short Weekday { get; set; }

    public TimeOnly StartAt { get; set; }

    public TimeOnly EndAt { get; set; }
}

public class UpdateEmployeeScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<UpdateEmployeeScheduleCommand, bool>
{
    public async Task<bool> Handle(
        UpdateEmployeeScheduleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar autorización
        await authorizationService.GetCurrentRoleAsync();

        // 2. Buscar horario
        var schedule = await unitOfWork
            .Repository<ScheduleEntity>()
            .GetByIdAsync(request.Id);

        if (schedule is null)
        {
            return false;
        }

        // 3. Aplicar reglas de negocio
        EmployeeScheduleRules.Validate(
            request.Weekday,
            request.StartAt,
            request.EndAt);

        // 4. Verificar empleado
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

        // 5. Consultar otros horarios del empleado
        var schedules = await unitOfWork
            .Repository<ScheduleEntity>()
            .FindAsync(s =>
                s.EmployeeId == request.EmployeeId &&
                s.Weekday == request.Weekday &&
                s.Id != request.Id);

        // 6. Validar superposición
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

        // 7. Actualizar horario
        schedule.EmployeeId = request.EmployeeId;
        schedule.Weekday = request.Weekday;
        schedule.StartAt = request.StartAt;
        schedule.EndAt = request.EndAt;

        unitOfWork.Repository<ScheduleEntity>()
            .Update(schedule);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
