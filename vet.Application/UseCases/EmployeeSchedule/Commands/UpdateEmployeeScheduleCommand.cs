using System.ComponentModel.DataAnnotations;
using MediatR;
using vet.Application.Validators.Employee;
using vet.Application.Validators.EmployeeSchedule;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Commands;

public class UpdateEmployeeScheduleCommand : IRequest<bool>
{
    // Se asigna desde la URL, no desde el JSON
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
    EmployeeScheduleValidator scheduleValidator,
    EmployeePermissionValidator permissionValidator
) : IRequestHandler<UpdateEmployeeScheduleCommand, bool>
{
    public async Task<bool> Handle(
        UpdateEmployeeScheduleCommand request,
        CancellationToken cancellationToken)
    {
        await permissionValidator.ValidateManageAsync();

        var schedule = await unitOfWork
            .Repository<ScheduleEntity>()
            .GetByIdAsync(request.Id);

        if (schedule is null)
            return false;

        await scheduleValidator.ValidateAsync(
            request.EmployeeId,
            request.Weekday,
            request.StartAt,
            request.EndAt,
            request.Id);

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