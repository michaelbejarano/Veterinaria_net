using MediatR;
using vet.Domain.Ports;

using OwnerEntity = vet.Domain.Entities.Owner;

namespace vet.Application.UseCases.Owner.Queries;

public class GetOwnerByIdQuery : IRequest<OwnerDto>
{
    public long Id { get; set; }
}

public class GetOwnerByIdQueryHandler
    : IRequestHandler<GetOwnerByIdQuery, OwnerDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOwnerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OwnerDto> Handle(
        GetOwnerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork
            .Repository<OwnerEntity>()
            .GetByIdAsync(request.Id);

        if (owner is null)
            throw new KeyNotFoundException(
                "El propietario no existe.");

        return new OwnerDto
        {
            Id = owner.Id,
            FirstNames = owner.FirstNames,
            DocumentType = owner.DocumentType,
            DocumentNumber = owner.DocumentNumber,
            Phone = owner.Phone,
            Email = owner.Email,
            Address = owner.Address,
            CreatedAt = owner.CreatedAt
        };
    }
}