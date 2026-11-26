using MediatR;
using vet.Domain.Ports;

using ScheduleBlockEntity = vet.Domain.Entities.ScheduleBlock;

namespace vet.Application.UseCases.ScheduleBlock.Commands;

public class DeleteScheduleBlockCommand : IRequest<Unit>
{
    public long Id { get; set; }
}

public class DeleteScheduleBlockCommandHandler
    : IRequestHandler<DeleteScheduleBlockCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteScheduleBlockCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        DeleteScheduleBlockCommand request,
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

        if (block.EndAt <= DateTime.UtcNow)
            throw new ArgumentException(
                "No se puede eliminar un bloqueo finalizado.");

        _unitOfWork
            .Repository<ScheduleBlockEntity>()
            .Delete(block);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Unit.Value;
    }
}