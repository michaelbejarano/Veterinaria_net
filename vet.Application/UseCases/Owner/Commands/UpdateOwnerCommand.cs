using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Owner;

using OwnerEntity = vet.Domain.Entities.Owner;

namespace vet.Application.UseCases.Owner.Commands;

public class UpdateOwnerCommand : IRequest<bool>
{
    public long Id { get; set; }

    public string FirstNames { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class UpdateOwnerCommandHandler
    : IRequestHandler<UpdateOwnerCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOwnerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        UpdateOwnerCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
            throw new ArgumentException(
                "El identificador del propietario no es válido.");

        var documentType = OwnerRules
            .NormalizeOptional(request.DocumentType)?
            .ToUpperInvariant();

        var documentNumber = OwnerRules
            .NormalizeOptional(request.DocumentNumber);

        var phone = OwnerRules
            .NormalizeOptional(request.Phone);

        var email = OwnerRules
            .NormalizeOptional(request.Email);

        // 1. Validar datos

        OwnerRules.Validate(
            request.FirstNames,
            documentType,
            documentNumber,
            phone,
            email);

        var repository = _unitOfWork
            .Repository<OwnerEntity>();

        // 2. Buscar propietario

        var owner = await repository
            .GetByIdAsync(request.Id);

        if (owner is null)
            throw new KeyNotFoundException(
                "El propietario no existe.");

        // 3. Verificar documento duplicado

        if (documentType is not null &&
            documentNumber is not null)
        {
            var existing = await repository.FindAsync(o =>
                o.Id != request.Id &&
                o.DocumentType == documentType &&
                o.DocumentNumber == documentNumber);

            if (existing.Any())
                throw new ArgumentException(
                    "El documento pertenece a otro propietario.");
        }

        // 4. Actualizar datos

        owner.FirstNames = request.FirstNames.Trim();
        owner.DocumentType = documentType;
        owner.DocumentNumber = documentNumber;
        owner.Phone = phone;
        owner.Email = email;
        owner.Address = OwnerRules.NormalizeOptional(
            request.Address);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}