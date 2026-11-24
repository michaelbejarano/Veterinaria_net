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

    [HttpPost("entries")]
    public async Task<IActionResult> RegisterEntry(
        [FromBody] RegisterInventoryEntryCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }

    [HttpGet("batches/{batchId:long}/stock")]
    public async Task<IActionResult> GetBatchStock(long batchId)
    {
        var result = await _mediator.Send(
            new GetBatchStockQuery(batchId));

        return Ok(result);
    }
}