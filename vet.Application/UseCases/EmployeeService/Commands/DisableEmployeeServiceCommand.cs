using MediatR;
using vet.Domain.Ports;
using EmployeeServiceEntity = vet.Domain.Entities.EmployeeService;

namespace vet.Application.UseCases.EmployeeService.Commands;

public class DisableEmployeeServiceCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DisableEmployeeServiceCommandHandler
    : IRequestHandler<DisableEmployeeServiceCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DisableEmployeeServiceCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DisableEmployeeServiceCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .GetByIdAsync(request.Id);

        if (assignment is null)
            return false;

        if (!assignment.IsEnabled)
            return true;

        assignment.IsEnabled = false;

        _unitOfWork
            .Repository<EmployeeServiceEntity>()
            .Update(assignment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}