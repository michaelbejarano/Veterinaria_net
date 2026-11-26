using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;

using CashRegisterEntity = vet.Domain.Entities.CashRegister;

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

    public CreateCashRegisterCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        CreateCashRegisterCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId <= 0)
            throw new ArgumentException(
                "La empresa es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException(
                "El nombre de la caja es obligatorio.");

        var name = request.Name.Trim();

        if (name.Length > 100)
            throw new ArgumentException(
                "El nombre admite máximo 100 caracteres.");

        // 1. Verificar empresa

        var company = await _unitOfWork
            .Repository<Company>()
            .GetByIdAsync(request.CompanyId);

        if (company is null)
            throw new KeyNotFoundException(
                "La empresa no existe.");

        // 2. Verificar nombre duplicado

        var repository = _unitOfWork
            .Repository<CashRegisterEntity>();

        var existing = await repository.FindAsync(c =>
            c.CompanyId == request.CompanyId &&
            c.Name == name);

        if (existing.Any())
            throw new ArgumentException(
                "Ya existe una caja con ese nombre.");

        // 3. Crear caja

        var cashRegister = new CashRegisterEntity
        {
            CompanyId = request.CompanyId,
            Name = name,
            IsActive = true
        };

        await repository.AddAsync(cashRegister);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return cashRegister.Id;
    }
}