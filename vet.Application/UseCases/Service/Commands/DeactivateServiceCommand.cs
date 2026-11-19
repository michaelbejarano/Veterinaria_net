using MediatR;
using vet.Domain.Ports;
using ServiceEntity = vet.Domain.Entities.Service;

namespace vet.Application.UseCases.Service.Commands;

public class DeactivateServiceCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeactivateServiceCommandHandler
    : IRequestHandler<DeactivateServiceCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateServiceCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeactivateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _unitOfWork
            .Repository<ServiceEntity>()
            .GetByIdAsync(request.Id);

        if (service is null)
            return false;

        if (!service.IsActive)
            return true;

        service.IsActive = false;

        _unitOfWork.Repository<ServiceEntity>().Update(service);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}