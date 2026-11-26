using MediatR;
using vet.Domain.Ports;

using RegisterEntity = vet.Domain.Entities.CashRegister;
using SessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.CashRegister.Queries;

public record GetCashRegisterByIdQuery(long Id)
    : IRequest<CashRegisterDto>;

public class GetCashRegisterByIdQueryHandler
    : IRequestHandler<GetCashRegisterByIdQuery, CashRegisterDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashRegisterByIdQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashRegisterDto> Handle(
        GetCashRegisterByIdQuery request,
        CancellationToken cancellationToken)
    {
        var register = await _unitOfWork
            .Repository<RegisterEntity>()
            .GetByIdAsync(request.Id);

        if (register is null)
            throw new KeyNotFoundException(
                "La caja no existe.");

        var sessions = await _unitOfWork
            .Repository<SessionEntity>()
            .FindAsync(x =>
                x.CashRegisterId == register.Id &&
                x.Status == "OPEN");

        return new CashRegisterDto
        {
            Id = register.Id,
            CompanyId = register.CompanyId,
            Name = register.Name,
            IsActive = register.IsActive,
            OpenSessionId = sessions.SingleOrDefault()?.Id
        };
    }
}