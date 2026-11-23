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
}