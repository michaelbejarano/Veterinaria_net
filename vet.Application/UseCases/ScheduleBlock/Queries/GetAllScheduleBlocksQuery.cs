using MediatR;
using vet.Domain.Ports;

using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.ScheduleBlock.Queries;

public class GetAllScheduleBlocksQuery
    : IRequest<List<ScheduleBlockResponse>>
{
}

public class ScheduleBlockResponse
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string? Reason { get; set; }
}

public class GetAllScheduleBlocksQueryHandler
    : IRequestHandler<
        GetAllScheduleBlocksQuery,
        List<ScheduleBlockResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllScheduleBlocksQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ScheduleBlockResponse>> Handle(
        GetAllScheduleBlocksQuery request,
        CancellationToken cancellationToken)
    {
        var blocks = await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .GetAllAsync();

        return blocks
            .OrderByDescending(b => b.StartAt)
            .Select(b => new ScheduleBlockResponse
            {
                Id = b.Id,
                EmployeeId = b.EmployeeId,
                StartAt = b.StartAt,
                EndAt = b.EndAt,
                Reason = b.Reason
            })
            .ToList();
    }
}