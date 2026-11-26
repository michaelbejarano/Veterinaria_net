using MediatR;
using vet.Domain.Ports;

using SessionEntity = vet.Domain.Entities.CashSession;

namespace vet.Application.UseCases.CashSession.Queries;

public class GetAllCashSessionsQuery
    : IRequest<List<CashSessionDto>>
{
    public long? CashRegisterId { get; set; }
    public string? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class CashSessionDto
{
    public long Id { get; set; }
    public long CashRegisterId { get; set; }
    public long OpenedByUserId { get; set; }
    public long? ClosedByUserId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal? CountedCash { get; set; }
    public string Status { get; set; } = string.Empty;

    public static CashSessionDto FromEntity(SessionEntity session)
    {
        return new CashSessionDto
        {
            Id = session.Id,
            CashRegisterId = session.CashRegisterId,
            OpenedByUserId = session.OpenedByUserId,
            ClosedByUserId = session.ClosedByUserId,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            OpeningFloat = session.OpeningFloat,
            CountedCash = session.CountedCash,
            Status = session.Status
        };
    }
}

public class GetAllCashSessionsQueryHandler
    : IRequestHandler<GetAllCashSessionsQuery, List<CashSessionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCashSessionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CashSessionDto>> Handle(
        GetAllCashSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToUpperInvariant();

        if (status is not null &&
            status != "OPEN" &&
            status != "CLOSED")
            throw new ArgumentException("Estado de caja inválido.");

        if (request.FromDate.HasValue &&
            request.ToDate.HasValue &&
            request.FromDate.Value > request.ToDate.Value)
            throw new ArgumentException("Rango de fechas inválido.");

        if (request.ToDate == DateOnly.MaxValue)
            throw new ArgumentException("Fecha final inválida.");

        var timeZone = TimeZoneInfo
            .FindSystemTimeZoneById("America/Lima");

        DateTime? fromUtc = request.FromDate.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(
                request.FromDate.Value
                    .ToDateTime(TimeOnly.MinValue),
                timeZone)
            : null;

        DateTime? toUtc = request.ToDate.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(
                request.ToDate.Value.AddDays(1)
                    .ToDateTime(TimeOnly.MinValue),
                timeZone)
            : null;

        var sessions = await _unitOfWork
            .Repository<SessionEntity>()
            .FindAsync(x =>
                (!request.CashRegisterId.HasValue ||
                 x.CashRegisterId == request.CashRegisterId.Value) &&
                (status == null || x.Status == status) &&
                (!fromUtc.HasValue || x.OpenedAt >= fromUtc.Value) &&
                (!toUtc.HasValue || x.OpenedAt < toUtc.Value));

        return sessions
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .Select(CashSessionDto.FromEntity)
            .ToList();
    }
}