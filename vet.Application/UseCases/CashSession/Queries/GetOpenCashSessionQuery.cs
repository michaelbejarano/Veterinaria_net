using MediatR;
using vet.Domain.Ports;

using RegisterEntity = vet.Domain.Entities.CashRegister;
using SessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.CashSession.Queries;

public record GetOpenCashSessionQuery(long CashRegisterId)
    : IRequest<CashSessionDto?>;

public class GetOpenCashSessionQueryHandler
    : IRequestHandler<GetOpenCashSessionQuery, CashSessionDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOpenCashSessionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashSessionDto?> Handle(
        GetOpenCashSessionQuery request,
        CancellationToken cancellationToken)
    {
        var register = await _unitOfWork
            .Repository<RegisterEntity>()
            .GetByIdAsync(request.CashRegisterId);

        if (register is null)
            throw new KeyNotFoundException("La caja no existe.");

        var sessions = await _unitOfWork
            .Repository<SessionEntity>()
            .FindAsync(x =>
                x.CashRegisterId == request.CashRegisterId &&
                x.Status == "OPEN");

        var session = sessions.SingleOrDefault();

        return session is null
            ? null
            : CashSessionDto.FromEntity(session);
    }
}