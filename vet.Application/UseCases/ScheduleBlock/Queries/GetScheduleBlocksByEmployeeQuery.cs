using MediatR;
using vet.Domain.Ports;

using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;
using EmployeeEntity = vet.Domain.Entities.Employee;

namespace vet.Application.UseCases.ScheduleBlock.Queries;

public class GetScheduleBlocksByEmployeeQuery
    : IRequest<List<ScheduleBlockResponse>>
{
    public long EmployeeId { get; set; }
}

public class GetScheduleBlocksByEmployeeQueryHandler
    : IRequestHandler<
        GetScheduleBlocksByEmployeeQuery,
        List<ScheduleBlockResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetScheduleBlocksByEmployeeQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ScheduleBlockResponse>> Handle(
        GetScheduleBlocksByEmployeeQuery request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeId <= 0)
            throw new ArgumentException(
                "El trabajador es obligatorio.");

        var employee = await _unitOfWork
            .Repository<EmployeeEntity>()
            .GetByIdAsync(request.EmployeeId);

        if (employee is null)
            throw new KeyNotFoundException(
                "El trabajador no existe.");

        var blocks = await _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .FindAsync(b =>
                b.EmployeeId == request.EmployeeId);

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