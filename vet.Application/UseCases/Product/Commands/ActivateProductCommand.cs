using MediatR;
using vet.Domain.Ports;

using ProductEntity = vet.Domain.Entities.Product;

namespace vet.Application.UseCases.Product.Commands;

public class ActivateProductCommand : IRequest<bool>
{
    public long Id { get; set; }
}

public class ActivateProductCommandHandler : IRequestHandler<ActivateProductCommand, bool> { private readonly IUnitOfWork _unitOfWork; public ActivateProductCommandHandler(IUnitOfWork unitOfWork)
    
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        ActivateProductCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ArgumentException(
                "El identificador del producto no es válido.");
        }

        var product = await _unitOfWork
            .Repository<ProductEntity>()
            .GetByIdAsync(request.Id);

        if (product is null)
        {
            throw new KeyNotFoundException(
                "El producto no existe.");
        }

        if (product.IsActive)
        {
            return true;
        }

        product.IsActive = true;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}