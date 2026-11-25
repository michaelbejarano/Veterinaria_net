using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Pet.Commands;
using vet.Application.UseCases.Pet.Queries;
using vet.Application.UseCases.Pet.Querys;

namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/pets")]
public class PetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePetCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pets = await _mediator.Send(
            new GetAllPetsQuery());

        return Ok(pets);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var pet = await _mediator.Send(
            new GetPetByIdQuery { Id = id });

        if (pet is null)
            return NotFound(new
            {
                message = "Mascota no encontrada."
            });

        return Ok(pet);
    }
    
    
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdatePetCommand command)
    {
        command.Id = id;

        var updated = await _mediator.Send(command);

        if (!updated)
            return NotFound(new
            {
                message = "Mascota no encontrada."
            });

        return NoContent();
    }

    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id)
    {
        var deactivated = await _mediator.Send(
            new DeactivatePetCommand { Id = id });

        if (!deactivated)
            return NotFound(new
            {
                message = "Mascota no encontrada."
            });

        return NoContent();
    }

    [HttpGet("{id:long}/clinical-history")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClinicalHistory(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPetClinicalHistoryQuery
            {
                PetId = id
            },
            cancellationToken);

        return Ok(result);
    }
}