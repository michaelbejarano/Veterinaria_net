
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.EmployeeSchedule.Commands;
using vet.Application.UseCases.EmployeeSchedule.Querys;

namespace vet_api.Controllers;

[ApiController]
[Route("api/employee-schedules")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
public class EmployeeSchedulesController(
    IMediator mediator
) : ControllerBase
{
    // GET: todos los horarios
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(
            new GetAllEmployeeSchedulesQuery());

        return Ok(result);
    }

    // GET: horario por ID
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await mediator.Send(
            new GetEmployeeScheduleByIdQuery(id));

        if (result is null)
            return NotFound(new
            {
                message = "Horario no encontrado."
            });

        return Ok(result);
    }

    // GET: horarios de un empleado
    [HttpGet("employee/{employeeId:long}")]
    public async Task<IActionResult> GetByEmployee(
        long employeeId)
    {
        var result = await mediator.Send(
            new GetEmployeeSchedulesByEmployeeQuery(employeeId));

        return Ok(result);
    }

    // POST: registrar horario
    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeScheduleCommand command)
    {
        var id = await mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    // PUT: actualizar horario
    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateEmployeeScheduleCommand command)
    {
        if (id <= 0)
            return BadRequest(new
            {
                message = "ID de horario inválido."
            });

        command.Id = id;

        var updated = await mediator.Send(command);

        if (!updated)
            return NotFound(new
            {
                message = "Horario no encontrado."
            });

        return NoContent();
    }

    // DELETE: eliminar horario
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await mediator.Send(
            new DeleteEmployeeScheduleCommand
            {
                Id = id
            });

        if (!deleted)
            return NotFound(new
            {
                message = "Horario no encontrado."
            });

        return NoContent();
    }
}
