using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.ServiceSupply;

using SupplyEntity = vet.Domain.Entities.ServiceSupply;

namespace vet.Application.UseCases.ServiceSupply.Commands;

public class DeleteServiceSupplyCommand : IRequest<bool>
{
    public long ServiceId { get; set; }
    public long ProductId { get; set; }
}

public class DeleteServiceSupplyCommandHandler
    : IRequestHandler<DeleteServiceSupplyCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceSupplyCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteServiceSupplyCommand request,
        CancellationToken cancellationToken)
    {
        ServiceSupplyRules.ValidateIdentifiers(
            request.ServiceId,
            request.ProductId);

        // 1. Buscar asignación

        var repository = _unitOfWork
            .Repository<SupplyEntity>();

        var supplies = await repository.FindAsync(s =>
            s.ServiceId == request.ServiceId &&
            s.ProductId == request.ProductId);

        var supply = supplies.SingleOrDefault();

        if (supply is null)
            throw new KeyNotFoundException(
                "El insumo no está asignado al servicio.");

        // 2. Eliminar asociación

        repository.Delete(supply);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}