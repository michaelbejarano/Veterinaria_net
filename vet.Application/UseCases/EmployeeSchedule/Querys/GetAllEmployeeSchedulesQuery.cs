using MediatR;
using vet.Domain.DTOs.EmployeeSchedule;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Querys;

public record GetAllEmployeeSchedulesQuery()
    : IRequest<IEnumerable<EmployeeScheduleDto>>;

public class GetAllEmployeeSchedulesQueryHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<GetAllEmployeeSchedulesQuery,
    IEnumerable<EmployeeScheduleDto>>
{
    public async Task<IEnumerable<EmployeeScheduleDto>> Handle(
        GetAllEmployeeSchedulesQuery request,
        CancellationToken cancellationToken)
    {
        var schedules = await unitOfWork
            .Repository<ScheduleEntity>()
            .GetAllAsync();

        return schedules
            .OrderBy(s => s.EmployeeId)
            .ThenBy(s => s.Weekday)
            .ThenBy(s => s.StartAt)
            .Select(s => new EmployeeScheduleDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                Weekday = s.Weekday,
                StartAt = s.StartAt,
                EndAt = s.EndAt
            });
    }
}