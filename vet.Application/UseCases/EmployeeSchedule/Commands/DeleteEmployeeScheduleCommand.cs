using MediatR;
using vet.Application.Services;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Commands;

public class DeleteEmployeeScheduleCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeleteEmployeeScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    UserAuthorizationService authorizationService
) : IRequestHandler<DeleteEmployeeScheduleCommand, bool>
{
    public async Task<bool> Handle(
        DeleteEmployeeScheduleCommand request,
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

        // 3. Eliminar horario
        unitOfWork.Repository<ScheduleEntity>()
            .Delete(schedule);

        // 4. Guardar cambios
        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}