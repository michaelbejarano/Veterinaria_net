using MediatR;
using vet.Domain.Ports;
using vet.Domain.Services;

using CashRegisterEntity = vet.Domain.Entities.CashRegister;
using CashSessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.CashSession.Commands;

public class OpenCashSessionCommand : IRequest<long>
{
    public long CashRegisterId { get; set; }
    public decimal OpeningFloat { get; set; }
    public long OpenedByUserId { get; set; }
}

public class OpenCashSessionCommandHandler
    : IRequestHandler<OpenCashSessionCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public OpenCashSessionCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(
        OpenCashSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CashRegisterId <= 0)
            throw new ArgumentException(
                "La caja es obligatoria.");

        if (request.OpenedByUserId <= 0)
            throw new ArgumentException(
                "El usuario autenticado no es válido.");

        CashSessionRules.ValidateMoney(
            request.OpeningFloat);

        // 1. Verificar caja

        var cashRegister = await _unitOfWork
            .Repository<CashRegisterEntity>()
            .GetByIdAsync(request.CashRegisterId);

        if (cashRegister is null)
            throw new KeyNotFoundException(
                "La caja no existe.");

        if (!cashRegister.IsActive)
            throw new ArgumentException(
                "La caja está inactiva.");

        // 2. Verificar sesión abierta

        var repository = _unitOfWork
            .Repository<CashSessionEntity>();

        var sessions = await repository.FindAsync(s =>
            s.CashRegisterId == request.CashRegisterId &&
            s.Status == "OPEN");

        if (sessions.Any())
            throw new ArgumentException(
                "Esta caja ya tiene una sesión abierta.");

        // 3. Abrir sesión

        var session = new CashSessionEntity
        {
            CashRegisterId = request.CashRegisterId,
            OpenedByUserId = request.OpenedByUserId,
            OpeningFloat = request.OpeningFloat,
            OpenedAt = DateTime.UtcNow,
            Status = "OPEN"
        };

        await repository.AddAsync(session);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return session.Id;
    }
}