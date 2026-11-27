
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using vet.Application.UseCases.Species.Querys;
using vet.Application.UseCases.Breed.Querys;
using vet.Application.UseCases.Species.Commands;
using vet.Application.UseCases.Breed.Commands;

namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/species")]
public class SpeciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpeciesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var species = await _mediator.Send(
            new GetAllSpeciesQuery(),
            cancellationToken);

        return Ok(species);
    }

    [HttpGet("{speciesId:long}/breeds")]
    public async Task<IActionResult> GetBreeds(
        long speciesId,
        CancellationToken cancellationToken)
    {
        var breeds = await _mediator.Send(
            new GetBreedsBySpeciesQuery
            {
                SpeciesId = speciesId
            },
            cancellationToken);

        return Ok(breeds);
    }

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> CreateSpecies(
        [FromBody] CreateSpeciesCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _mediator.Send(
                command,
                cancellationToken);

            return StatusCode(201, new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{speciesId:long}/breeds")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> CreateBreed(
        long speciesId,
        [FromBody] CreateBreedCommand command,
        CancellationToken cancellationToken)
    {
        command.SpeciesId = speciesId;

        try
        {
            var id = await _mediator.Send(
                command,
                cancellationToken);

            return StatusCode(201, new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
