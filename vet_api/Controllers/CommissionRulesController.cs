using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.CommissionSettlement.Commands;
using vet.Application.UseCases.CommissionSettlement.Queries;

namespace vet_api.Controllers;

[ApiController]
[Route("api/commission-settlements")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
public class CommissionSettlementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionSettlementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ========================================
    // 1. OBTENER COMISIONES PENDIENTES
    // ========================================

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(
        [FromQuery] long employeeId,
        [FromQuery] DateOnly? untilDate,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
            return BadRequest(new
            {
                message = "El trabajador es obligatorio."
            });

        var result = await _mediator.Send(
            new GetPendingCommissionsQuery
            {
                EmployeeId = employeeId,
                UntilDate = untilDate
            },
            cancellationToken);

        return Ok(result);
    }

    // ========================================
    // 2. LISTAR LIQUIDACIONES
    // ========================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllCommissionSettlementsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            query,
            cancellationToken);

        return Ok(result);
    }

    // ========================================
    // 3. OBTENER LIQUIDACIÓN POR ID
    // ========================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCommissionSettlementByIdQuery
            {
                Id = id
            },
            cancellationToken);

        return Ok(result);
    }

    // ========================================
    // 4. CREAR LIQUIDACIÓN
    // ========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCommissionSettlementCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new
            {
                id,
                message = "Liquidación creada correctamente."
            });
    }

    // ========================================
    // 5. PAGAR LIQUIDACIÓN
    // ========================================

    [HttpPost("{id:long}/pay")]
    public async Task<IActionResult> Pay(
        long id,
        [FromBody] PayCommissionSettlementCommand command,
        CancellationToken cancellationToken)
    {
        command.SettlementId = id;

        var result = await _mediator.Send(
            command,
            cancellationToken);

        return Ok(result);
    }
}