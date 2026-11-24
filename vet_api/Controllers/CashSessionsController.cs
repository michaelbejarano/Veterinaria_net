using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.CashSession.Commands;

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

    [HttpPost("open")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Open(
        [FromBody] OpenCashSessionCommand command)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario."
            });

        // Siempre sobrescribimos el ID recibido.
        command.OpenedByUserId = userId;

        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id }
        );
    }
    
    [HttpPost("{id:long}/close")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
    public async Task<IActionResult> Close(
        long id,
        [FromBody] CloseCashSessionCommand command)
    {
        var userIdValue =
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (!long.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.CashSessionId = id;
        command.ClosedByUserId = userId;

        var result = await _mediator.Send(command);

        return Ok(result);
    }
    [HttpPost("{id:long}/movements")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterMovement(
        long id,
        [FromBody] RegisterCashMovementCommand command)
    {
        command.CashSessionId = id;

        var movementId = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id = movementId }
        );
    }
}