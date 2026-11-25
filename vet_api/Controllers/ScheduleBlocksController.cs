using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using vet.Application.UseCases.ScheduleBlock.Commands;
using vet.Application.UseCases.ScheduleBlock.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
[Route("api/schedule-blocks")]
public class ScheduleBlocksController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScheduleBlocksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateScheduleBlockCommand command,
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
                message = "Bloqueo creado correctamente."
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllScheduleBlocksQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetScheduleBlockByIdQuery
            {
                Id = id
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("employee/{employeeId:long}")]
    public async Task<IActionResult> GetByEmployee(
        long employeeId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetScheduleBlocksByEmployeeQuery
            {
                EmployeeId = employeeId
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateScheduleBlockCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;

        await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            message = "Bloqueo actualizado correctamente."
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteScheduleBlockCommand
            {
                Id = id
            },
            cancellationToken);

        return NoContent();
    }
}