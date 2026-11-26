using MediatR;
using vet.Domain.Ports;

using SessionEntity = vet.Domain.Entities.CashSession;
using MovementEntity = vet.Domain.Entities.CashMovement;

namespace vet.Application.UseCases.CashSession.Queries;

public class GetCashSessionMovementsQuery
    : IRequest<List<CashMovementDto>>
{
    public long CashSessionId { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Method { get; set; }
}

public class CashMovementDto
{
    public long Id { get; set; }
    public long CashSessionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long? PaymentId { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
}

public class GetCashSessionMovementsQueryHandler
    : IRequestHandler<
        GetCashSessionMovementsQuery,
        List<CashMovementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashSessionMovementsQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CashMovementDto>> Handle(
        GetCashSessionMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _unitOfWork
            .Repository<SessionEntity>()
            .GetByIdAsync(request.CashSessionId);

        if (session is null)
            throw new KeyNotFoundException(
                "La sesión de caja no existe.");

        var type = Normalize(request.Type);
        var category = Normalize(request.Category);
        var method = Normalize(request.Method);

        if (type is not null &&
            type != "INCOME" &&
            type != "EXPENSE")
            throw new ArgumentException(
                "Tipo de movimiento inválido.");

        var movements = await _unitOfWork
            .Repository<MovementEntity>()
            .FindAsync(x =>
                x.CashSessionId == request.CashSessionId &&
                (type == null || x.Type == type) &&
                (category == null || x.Category == category) &&
                (method == null || x.Method == method));

        return movements
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Select(x => new CashMovementDto
            {
                Id = x.Id,
                CashSessionId = x.CashSessionId,
                Type = x.Type,
                Category = x.Category,
                Method = x.Method,
                Amount = x.Amount,
                PaymentId = x.PaymentId,
                Reference = x.Reference,
                Description = x.Description,
                Date = x.Date
            })
            .ToList();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }
}