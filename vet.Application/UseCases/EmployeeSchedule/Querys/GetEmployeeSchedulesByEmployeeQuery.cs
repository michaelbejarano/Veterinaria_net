using MediatR;
using vet.Domain.DTOs.EmployeeSchedule;
using vet.Domain.Ports;
using ScheduleEntity = vet.Domain.Entities.EmployeeSchedule;

namespace vet.Application.UseCases.EmployeeSchedule.Querys;

public record GetEmployeeSchedulesByEmployeeQuery(long EmployeeId)
    : IRequest<IEnumerable<EmployeeScheduleDto>>;

public class GetEmployeeSchedulesByEmployeeQueryHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<GetEmployeeSchedulesByEmployeeQuery,
    IEnumerable<EmployeeScheduleDto>>
{
    public async Task<IEnumerable<EmployeeScheduleDto>> Handle(
        GetEmployeeSchedulesByEmployeeQuery request,
        CancellationToken cancellationToken)
    {
        var schedules = await unitOfWork
            .Repository<ScheduleEntity>()
            .FindAsync(s => s.EmployeeId == request.EmployeeId);

        return schedules
            .OrderBy(s => s.Weekday)
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