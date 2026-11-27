using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Employee.Querys;
using vet.Application.UseCases.Employee.Commands;
using vet.Application.UseCases.Employee.Commands;
namespace vet_api.Controllers;
using vet.Application.UseCases.Employee.Commands;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
public class EmployeesController(IMediator mediator)
    : ControllerBase
{
    // =========================================
    // GET: api/employees
    // =========================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive = true)
    {
        var result = await mediator.Send(
            new GetAllEmployeesQuery(isActive));

        return Ok(result);
    }

    // =========================================
    // GET: api/employees/{id}
    // =========================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await mediator.Send(
            new GetEmployeeByIdQuery(id));

        if (result is null)
        {
            return NotFound(new
            {
                message = "Empleado no encontrado."
            });
        }

        return Ok(result);
    }
    
    
    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeCommand command)
    {
        var id = await mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id }
        );
    }
    
    
    
    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateEmployeeCommand command)
    {
        // Validar ID recibido por la URL
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "El ID del empleado debe ser mayor a cero."
            });
        }

        // Asignar ID al Command
        command.Id = id;

        // Ejecutar Handler mediante MediatR
        var updated = await mediator.Send(command);

        if (!updated)
        {
            return NotFound(new
            {
                message = "Empleado no encontrado."
            });
        }

        return NoContent();
    }

    
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await mediator.Send(
            new DeleteEmployeeCommand
            {
                Id = id
            });

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Empleado no encontrado."
            });
        }

        return NoContent();
    }
    // =========================================
// REACTIVAR TRABAJADOR
// =========================================

    [HttpPatch("{id:long}/activate")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Activate(long id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "El ID del empleado debe ser mayor a cero."
            });
        }

        try
        {
            var activated = await mediator.Send(
                new ActivateEmployeeCommand
                {
                    Id = id
                });

            if (!activated)
            {
                return NotFound(new
                {
                    message = "Empleado no encontrado."
                });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message
            });
        }
    }


}