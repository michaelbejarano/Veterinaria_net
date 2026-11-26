
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Sale.Commands;
using vet.Application.UseCases.Sale.Queries;

namespace vet_api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ========================================
    // 1. CREAR VENTA
    // ========================================
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.SaleId },
            result);
    }

    // ========================================
    // 2. CONFIRMAR VENTA
    // ========================================
    [HttpPost("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(
        long id,
        [FromBody] ConfirmSaleCommand command,
        CancellationToken cancellationToken)
    {
        command.SaleId = id;

        var result = await _mediator.Send(
            command,
            cancellationToken);

        return Ok(result);
    }

    // ========================================
    // 3. EMITIR RECIBO INTERNO
    // ========================================
    [HttpPost("{id:long}/receipt")]
    public async Task<IActionResult> IssueReceipt(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new IssueInternalReceiptCommand
        {
            SaleId = id
        };

        var result = await _mediator.Send(
            command,
            cancellationToken);

        return Ok(result);
    }

    // ========================================
    // 4. LISTAR VENTAS
    // ========================================
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllSalesQuery(companyId),
            cancellationToken);

        return Ok(result);
    }

    // ========================================
    // 5. OBTENER VENTA POR ID
    // ========================================
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSaleByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }
    
    [HttpPatch("{id:long}/cancel")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Cancel(
        long id,
        [FromBody] CancelSaleCommand command,
        CancellationToken cancellationToken)
    {
        command.SaleId = id;

        await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            message = "Venta anulada correctamente."
        });
    }
}
