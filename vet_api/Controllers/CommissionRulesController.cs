using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.CommissionRule.Commands;
using vet.Application.UseCases.CommissionRule.Queries;

namespace vet_api.Controllers;

[ApiController]
[Route("api/commission-rules")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
public class CommissionRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // 1. Crear regla

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCommissionRuleCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new
            {
                id,
                message = "Regla de comisión creada correctamente."
            });
    }

    // 2. Obtener todas las reglas

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllCommissionRulesQuery(),
            cancellationToken);

        return Ok(result);
    }

    // 3. Obtener regla por ID

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCommissionRuleByIdQuery
            {
                Id = id
            },
            cancellationToken);

        return Ok(result);
    }

    // 4. Obtener reglas por trabajador-servicio

    [HttpGet("employee-service/{employeeServiceId:long}")]
    public async Task<IActionResult> GetByEmployeeService(
        long employeeServiceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCommissionRulesByEmployeeServiceQuery
            {
                EmployeeServiceId = employeeServiceId
            },
            cancellationToken);

        return Ok(result);
    }

    // 5. Actualizar regla

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateCommissionRuleCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;

        await _mediator.Send(
            command, cancellationToken);

        return Ok(new
        {
            message = "Regla de comisión actualizada correctamente."
        });
    }

    // 6. Eliminar regla

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteCommissionRuleCommand
            {
                Id = id
            },
            cancellationToken);

        return NoContent();
    }
}