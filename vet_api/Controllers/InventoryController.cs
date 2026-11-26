using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Inventory.Commands;
using vet.Application.UseCases.Inventory.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Registrar entrada de inventario

    [HttpPost("entries")]
    public async Task<IActionResult> RegisterEntry(
        [FromBody] RegisterInventoryEntryCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }

    // Registrar ajuste de inventario

    [HttpPost("adjustments")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> RegisterAdjustment(
        [FromBody] RegisterInventoryAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                id,
                message = "Ajuste registrado correctamente."
            });
    }

    // Consultar stock por lote

    [HttpGet("batches/{batchId:long}/stock")]
    public async Task<IActionResult> GetBatchStock(
        long batchId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBatchStockQuery(batchId),
            cancellationToken);

        return Ok(result);
    }

    // Consultar stock por producto

    [HttpGet("products/{productId:long}/stock")]
    public async Task<IActionResult> GetProductStock(
        long productId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProductStockQuery(productId),
            cancellationToken);

        return Ok(result);
    }

    // Consultar historial de movimientos

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] GetInventoryMovementsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            query, cancellationToken);

        return Ok(result);
    }

    // Consultar lotes vencidos y próximos a vencer

    [HttpGet("expiring-batches")]
    public async Task<IActionResult> GetExpiringBatches(
        [FromQuery] GetExpiringBatchesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            query, cancellationToken);

        return Ok(result);
    }
}