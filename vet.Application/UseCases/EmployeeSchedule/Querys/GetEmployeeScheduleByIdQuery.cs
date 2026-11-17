using MediatR;
using vet.Domain.DTOs.EmployeeSchedule;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Querys;

public record GetEmployeeScheduleByIdQuery(long Id)
    : IRequest<EmployeeScheduleDto?>;

public class GetEmployeeScheduleByIdQueryHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<GetEmployeeScheduleByIdQuery,
    EmployeeScheduleDto?>
{
    public async Task<EmployeeScheduleDto?> Handle(
        GetEmployeeScheduleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var schedule = await unitOfWork
            .Repository<ScheduleEntity>()
            .GetByIdAsync(request.Id);

        if (schedule is null)
            return null;

        return new EmployeeScheduleDto
        {
            Id = schedule.Id,
            EmployeeId = schedule.EmployeeId,
            Weekday = schedule.Weekday,
            StartAt = schedule.StartAt,
            EndAt = schedule.EndAt
        };
    }
}