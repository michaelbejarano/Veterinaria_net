using MediatR;
using vet.Domain.Ports;
using PetEntity = vet.Domain.Entities.Pet;

namespace vet.Application.UseCases.Pet.Commands;

public class DeactivatePetCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class DeactivatePetCommandHandler
    : IRequestHandler<DeactivatePetCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivatePetCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeactivatePetCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _unitOfWork
            .Repository<PetEntity>()
            .GetByIdAsync(request.Id);

        if (pet is null)
            return false;

        if (!pet.IsActive)
            return true;

        pet.IsActive = false;

        _unitOfWork.Repository<PetEntity>().Update(pet);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}