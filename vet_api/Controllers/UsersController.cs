using MediatR;
using Microsoft.AspNetCore.Mvc;
using vet.Domain.DTOs;
using vet.Application.UseCases.User.Querys;

namespace vet_api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(
        CancellationToken cancellationToken)
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
}