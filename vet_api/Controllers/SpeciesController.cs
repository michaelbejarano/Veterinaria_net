using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Species.Querys;
using vet.Application.UseCases.Breed.Querys;

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
    public async Task<IActionResult> GetAll()
    {
        var species = await _mediator.Send(
            new GetAllSpeciesQuery());

        return Ok(species);
    }

    [HttpGet("{speciesId:long}/breeds")]
    public async Task<IActionResult> GetBreeds(long speciesId)
    {
        var breeds = await _mediator.Send(
            new GetBreedsBySpeciesQuery
            {
                SpeciesId = speciesId
            });

        return Ok(breeds);
    }
}