using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.UseCases.CashRegister.Commands;

public class CreateCashRegisterCommand : IRequest<long>
{
    public long CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateCashRegisterCommandHandler
    : IRequestHandler<CreateCashRegisterCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashRegisterCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateCashRegisterCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validaciones específicas del caso de uso
        if (request.CompanyId <= 0)
            throw new ArgumentException("La empresa es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("El nombre de la caja es obligatorio.");

        // 2. Verificar que exista la empresa
        var company = await _unitOfWork
            .Repository<Company>()
            .GetByIdAsync(request.CompanyId);

        if (company is null)
            throw new KeyNotFoundException("La empresa no existe.");

        // 3. Crear la caja
        var cashRegister = new vet.Domain.Entities.CashRegister
        {
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            IsActive = true
        };

        // 4. Guardar utilizando el repositorio y UnitOfWork
        await _unitOfWork
            .Repository<vet.Domain.Entities.CashRegister>()
            .AddAsync(cashRegister);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Devolver el ID generado
        return cashRegister.Id;
    }
}