using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.CashRegister.Commands;
using vet.Application.UseCases.CashRegister.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
[Route("api/cash-registers")]
public class CashRegistersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashRegistersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCashRegisterCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllCashRegistersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCashRegisterByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }
}