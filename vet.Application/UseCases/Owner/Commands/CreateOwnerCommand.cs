using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services.Owner;

using OwnerEntity = vet.Domain.Entities.Owner;

namespace vet.Application.UseCases.Owner.Commands;

public class CreateOwnerCommand : IRequest<long>
{
    public string FirstNames { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class CreateOwnerCommandHandler
    : IRequestHandler<CreateOwnerCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOwnerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateOwnerCommand request,
        CancellationToken cancellationToken)
    {
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

        // 2. Verificar documento duplicado

        if (documentType is not null &&
            documentNumber is not null)
        {
            var existing = await repository.FindAsync(o =>
                o.DocumentType == documentType &&
                o.DocumentNumber == documentNumber);

            if (existing.Any())
                throw new ArgumentException(
                    "Ya existe un propietario con ese documento.");
        }

        // 3. Crear propietario

        var owner = new OwnerEntity
        {
            FirstNames = request.FirstNames.Trim(),
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            Phone = phone,
            Email = email,
            Address = OwnerRules.NormalizeOptional(
                request.Address),
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(owner);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return owner.Id;
    }
}