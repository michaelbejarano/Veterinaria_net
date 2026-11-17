using MediatR;
using vet.Application.Validators.Employee;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Commands;

public class DeleteEmployeeScheduleCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteEmployeeScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    EmployeePermissionValidator permissionValidator
) : IRequestHandler<DeleteEmployeeScheduleCommand, bool>
{
    public async Task<bool> Handle(
        DeleteEmployeeScheduleCommand request,
        CancellationToken cancellationToken)
    {
        await permissionValidator.ValidateManageAsync();

        var schedule = await unitOfWork
            .Repository<ScheduleEntity>()
            .GetByIdAsync(request.Id);

        if (schedule is null)
            return false;

        unitOfWork.Repository<ScheduleEntity>()
            .Delete(schedule);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}