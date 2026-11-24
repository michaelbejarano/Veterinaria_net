using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Service.Commands;
using vet.Application.UseCases.Service.Querys;

namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _mediator.Send(
            new GetAllServicesQuery());

        return Ok(services);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var service = await _mediator.Send(
            new GetServiceByIdQuery { Id = id });

        if (service is null)
            return NotFound(new
            {
                message = "Servicio no encontrado."
            });

        return Ok(service);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateServiceCommand command)
    {
        command.Id = id;

        var updated = await _mediator.Send(command);

        if (!updated)
            return NotFound(new
            {
                message = "Servicio no encontrado."
            });

        return NoContent();
    }

    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id)
    {
        var deactivated = await _mediator.Send(
            new DeactivateServiceCommand { Id = id });

        if (!deactivated)
            return NotFound(new
            {
                message = "Servicio no encontrado."
            });

        return NoContent();
    }

}