
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.User.Commands;
using vet.Application.UseCases.User.Querys;
using vet.Domain.DTOs;

namespace vet_api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    // =========================================
    // GET: api/users
    // =========================================

    [HttpGet]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(
        typeof(IEnumerable<UserDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await mediator.Send(
            new GetAllUserQuery(),
            cancellationToken);

        return Ok(users);
    }

    // =========================================
    // GET: api/users/{id}
    // =========================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var user = await mediator.Send(
            new GetUserByIdQuery { Id = id },
            cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado."
            });
        }

        return Ok(user);
    }

    // =========================================
    // POST: api/users
    // =========================================

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }

    // =========================================
    // PUT: api/users/{id}
    // =========================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }

    // =========================================
    // DELETE: api/users/{id}
    // =========================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await mediator.Send(
                new DeleteUserCommand { Id = id },
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Usuario no encontrado."
                });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }
}
