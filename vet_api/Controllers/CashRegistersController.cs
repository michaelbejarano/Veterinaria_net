using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.CashRegister.Commands;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
[Route("api/cash-registers")]
public class CashRegistersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashRegistersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCashRegisterCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id }
        );
    }
}