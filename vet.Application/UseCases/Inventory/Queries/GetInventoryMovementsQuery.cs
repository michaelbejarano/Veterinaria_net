using MediatR;
using vet.Domain.Ports;

using BatchEntity = vet.Domain.Entities.ProductBatch;
using MovementEntity = vet.Domain.Entities.InventoryMovement;

namespace vet.Application.UseCases.Inventory.Queries;

public class GetInventoryMovementsQuery
    : IRequest<List<InventoryMovementDto>>
{
    public long? ProductId { get; set; }
    public long? BatchId { get; set; }
    public string? Type { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class InventoryMovementDto
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public long ProductId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Reference { get; set; }
    public long? VisitServiceId { get; set; }
    public long? SaleItemId { get; set; }
    public DateTime Date { get; set; }
}

public class GetInventoryMovementsQueryHandler
    : IRequestHandler<
        GetInventoryMovementsQuery,
        List<InventoryMovementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInventoryMovementsQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<InventoryMovementDto>> Handle(
        GetInventoryMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var type = string.IsNullOrWhiteSpace(request.Type)
            ? null
            : request.Type.Trim().ToUpperInvariant();

        if (type is not null &&
            type != "IN" &&
            type != "OUT" &&
            type != "ADJUSTMENT_IN" &&
            type != "ADJUSTMENT_OUT")
            throw new ArgumentException(
                "Tipo de movimiento inválido.");

        if (request.FromDate.HasValue &&
            request.ToDate.HasValue &&
            request.FromDate.Value > request.ToDate.Value)
            throw new ArgumentException(
                "El rango de fechas no es válido.");

        if (request.ToDate == DateOnly.MaxValue)
            throw new ArgumentException(
                "La fecha final no es válida.");

        // 1. Convertir fechas locales a UTC

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

        // 2. Filtrar lotes

        var batches = await _unitOfWork
            .Repository<BatchEntity>()
            .FindAsync(b =>
                (!request.ProductId.HasValue ||
                 b.ProductId == request.ProductId.Value) &&
                (!request.BatchId.HasValue ||
                 b.Id == request.BatchId.Value));

        if (!batches.Any())
            return new List<InventoryMovementDto>();

        var batchIds = batches
            .Select(b => b.Id)
            .ToList();

        var batchDictionary = batches
            .ToDictionary(b => b.Id);

        // 3. Filtrar movimientos

        var movements = await _unitOfWork
            .Repository<MovementEntity>()
            .FindAsync(m =>
                batchIds.Contains(m.BatchId) &&
                (type == null || m.Type == type) &&
                (!fromUtc.HasValue ||
                 m.Date >= fromUtc.Value) &&
                (!toUtc.HasValue ||
                 m.Date < toUtc.Value));

        // 4. Construir respuesta

        return movements
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .Select(m => new InventoryMovementDto
            {
                Id = m.Id,
                BatchId = m.BatchId,
                ProductId = batchDictionary[m.BatchId].ProductId,
                BatchCode = batchDictionary[m.BatchId].BatchCode,
                Type = m.Type,
                Quantity = m.Quantity,
                UnitCost = m.UnitCost,
                TotalCost = m.Quantity * m.UnitCost,
                Reference = m.Reference,
                VisitServiceId = m.VisitServiceId,
                SaleItemId = m.SaleItemId,
                Date = m.Date
            })
            .ToList();
    }
}