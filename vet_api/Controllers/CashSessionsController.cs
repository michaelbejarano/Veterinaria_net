using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.CashSession.Commands;
using vet.Application.UseCases.CashSession.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
[Route("api/cash-sessions")]
public class CashSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Abrir caja

    [HttpPost("open")]
    public async Task<IActionResult> Open(
        [FromBody] OpenCashSessionCommand command,
        CancellationToken cancellationToken)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!long.TryParse(userIdClaim, out var userId) ||
            userId <= 0)
            return Unauthorized();

        command.OpenedByUserId = userId;

        var id = await _mediator.Send(
            command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    // Cerrar caja

    [HttpPost("{id:long}/close")]
    public async Task<IActionResult> Close(
        long id,
        [FromBody] CloseCashSessionCommand command,
        CancellationToken cancellationToken)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!long.TryParse(userIdClaim, out var userId) ||
            userId <= 0)
            return Unauthorized();

        command.CashSessionId = id;
        command.ClosedByUserId = userId;

        var result = await _mediator.Send(
            command, cancellationToken);

        return Ok(result);
    }

    // Registrar movimiento manual

    [HttpPost("{id:long}/movements")]
    public async Task<IActionResult> RegisterMovement(
        long id,
        [FromBody] RegisterCashMovementCommand command,
        CancellationToken cancellationToken)
    {
        command.CashSessionId = id;

        var movementId = await _mediator.Send(
            command, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id = movementId });
    }

    // Consultar historial de sesiones

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllCashSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            query, cancellationToken);

        return Ok(result);
    }

    // Consultar sesión por ID

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCashSessionByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    // Consultar sesión abierta de una caja

    [HttpGet("open/{cashRegisterId:long}")]
    public async Task<IActionResult> GetOpen(
        long cashRegisterId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetOpenCashSessionQuery(cashRegisterId),
            cancellationToken);

        return Ok(new
        {
            hasOpenSession = result is not null,
            session = result
        });
    }

    // Consultar movimientos

    [HttpGet("{id:long}/movements")]
    public async Task<IActionResult> GetMovements(
        long id,
        [FromQuery] GetCashSessionMovementsQuery query,
        CancellationToken cancellationToken)
    {
        query.CashSessionId = id;

        var result = await _mediator.Send(
            query, cancellationToken);

        return Ok(result);
    }

    // Consultar resumen financiero

    [HttpGet("{id:long}/summary")]
    public async Task<IActionResult> GetSummary(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCashSessionSummaryQuery(id),
            cancellationToken);

        return Ok(result);
    }
}