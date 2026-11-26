using MediatR;
using vet.Domain.Ports;

using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.ScheduleBlock.Queries;

public class GetScheduleBlockByIdQuery
    : IRequest<ScheduleBlockResponse>
{
    public long Id { get; set; }
}

public class GetScheduleBlockByIdQueryHandler
    : IRequestHandler<
        GetScheduleBlockByIdQuery,
        ScheduleBlockResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetScheduleBlockByIdQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleBlockResponse> Handle(
        GetScheduleBlockByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
            throw new ArgumentException(
                "El bloqueo es obligatorio.");

        var block = await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .GetByIdAsync(request.Id);

        if (block is null)
            throw new KeyNotFoundException(
                "El bloqueo no existe.");

        return new ScheduleBlockResponse
        {
            Id = block.Id,
            EmployeeId = block.EmployeeId,
            StartAt = block.StartAt,
            EndAt = block.EndAt,
            Reason = block.Reason
        };
    }
}