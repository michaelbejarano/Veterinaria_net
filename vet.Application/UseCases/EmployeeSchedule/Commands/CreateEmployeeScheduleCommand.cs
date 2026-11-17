using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Validators.Employee;
using vet.Application.Validators.EmployeeSchedule;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Commands;

public class CreateEmployeeScheduleCommand : IRequest<long>
{
    [Range(1, long.MaxValue)]
    public long EmployeeId { get; set; }

    [Range(1, 7)]
    public short Weekday { get; set; }

    [Required]
    public TimeOnly StartAt { get; set; }

    [Required]
    public TimeOnly EndAt { get; set; }
}

public class CreateEmployeeScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    EmployeeScheduleValidator scheduleValidator,
    EmployeePermissionValidator permissionValidator
) : IRequestHandler<CreateEmployeeScheduleCommand, long>
{
    public async Task<long> Handle(
        CreateEmployeeScheduleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar permisos administrativos
        await permissionValidator.ValidateManageAsync();

        // 2. Validar reglas del horario
        await scheduleValidator.ValidateAsync(
            request.EmployeeId,
            request.Weekday,
            request.StartAt,
            request.EndAt);

        // 3. Crear entidad
        var schedule = new ScheduleEntity
        {
            EmployeeId = request.EmployeeId,
            Weekday = request.Weekday,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        // 4. Registrar mediante repositorio genérico
        await unitOfWork
            .Repository<ScheduleEntity>()
            .AddAsync(schedule);

        // 5. Guardar cambios
        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return schedule.Id;
    }
}