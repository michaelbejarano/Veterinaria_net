using MediatR;
using Microsoft.AspNetCore.Mvc;
using vet.Domain.DTOs;
using vet.Application.UseCases.User.Querys;
using vet.Application.UseCases.User.Commands;

namespace vet_api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await mediator.Send(
            new GetAllUserQuery(),
            cancellationToken);

        return Ok(users);
    }
    
    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var user = await mediator.Send(
            new GetUserByIdQuery { Id = id },
            cancellationToken);

        if (user is null)
            return NotFound(new
            {
                message = "Usuario no encontrado."
            });

        return Ok(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command,[FromServices] IWebHostEnvironment environment, CancellationToken cancellationToken)
    {
        // Temporal: solo permitir registros en desarrollo
        // hasta implementar autenticación y autorización.
        if (!environment.IsDevelopment())
            return StatusCode(403);

        try
        {
            var id = await mediator.Send(
                command,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new
                {
                    id,
                    message = "Usuario registrado correctamente."
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
    
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserCommand command, [FromServices] IWebHostEnvironment environment, CancellationToken cancellationToken)
    {
        // Protección temporal hasta implementar JWT
        if (!environment.IsDevelopment())
            return StatusCode(403);

        if (id != command.Id)
        {
            return BadRequest(new
            {
                message = "El ID de la URL no coincide con el del usuario."
            });
        }

        try
        {
            var updated = await mediator.Send(
                command,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Usuario no encontrado."
                });
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }


}