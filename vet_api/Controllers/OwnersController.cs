
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using vet.Application.UseCases.Owner.Commands;
using vet.Application.UseCases.Owner.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/owners")]
public class OwnersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OwnersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var owners = await _mediator.Send(
            new GetAllOwnersQuery
            {
                Search = search
            },
            cancellationToken
        );

        return Ok(owners);
    }
    
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        try
        {
            var owner = await _mediator.Send(
                new GetOwnerByIdQuery
                {
                    Id = id
                },
                cancellationToken
            );

            return Ok(owner);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Propietario no encontrado."
            });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOwnerCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command,
            cancellationToken
        );

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new
            {
                id,
                message = "Propietario registrado correctamente."
            }
        );
    }
    
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateOwnerCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;

        try
        {
            var updated = await _mediator.Send(
                command,
                cancellationToken
            );

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Propietario no encontrado."
                });
            }

            return Ok(new
            {
                message = "Propietario actualizado correctamente."
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Propietario no encontrado."
            });
        }
    }
    

    [HttpGet("{id:long}/pets")]
    public async Task<IActionResult> GetPets(
        long id,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var pets = await _mediator.Send(
                new GetOwnerPetsQuery
                {
                    OwnerId = id,
                    IsActive = isActive
                },
                cancellationToken
            );

            return Ok(pets);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Propietario no encontrado."
            });
        }
    }
}
