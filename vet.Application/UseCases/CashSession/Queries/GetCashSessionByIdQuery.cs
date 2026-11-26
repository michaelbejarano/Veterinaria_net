using MediatR;
using vet.Domain.Ports;

using SessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.CashSession.Queries;

public record GetCashSessionByIdQuery(long Id)
    : IRequest<CashSessionDto>;

public class GetCashSessionByIdQueryHandler
    : IRequestHandler<GetCashSessionByIdQuery, CashSessionDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashSessionByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashSessionDto> Handle(
        GetCashSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _unitOfWork
            .Repository<SessionEntity>()
            .GetByIdAsync(request.Id);

        if (session is null)
            throw new KeyNotFoundException(
                "La sesión de caja no existe.");

        return CashSessionDto.FromEntity(session);
    }
}