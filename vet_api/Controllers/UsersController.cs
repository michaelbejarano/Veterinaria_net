using MediatR;
using Microsoft.AspNetCore.Mvc;
using vet.Domain.DTOs;
using vet.Application.UseCases.User.Querys;
using vet.Application.UseCases.User.Commands;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await mediator.Send(command, cancellationToken);

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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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