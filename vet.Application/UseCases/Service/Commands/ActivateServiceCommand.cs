using MediatR;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Service.Commands;

public class ActivateServiceCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class ActivateServiceCommandHandler
    : IRequestHandler<ActivateServiceCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivateServiceCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        ActivateServiceCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar servicio

        var service = await _unitOfWork
            .Repository<vet.Domain.Entities.Service>()
            .GetByIdAsync(request.Id);

        if (service is null)
            return false;

        // 2. Verificar estado actual

        if (service.IsActive)
            return true;

        // 3. Reactivar servicio

        service.IsActive = true;

        _unitOfWork
            .Repository<vet.Domain.Entities.Service>()
            .Update(service);

        // 4. Guardar cambios

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}