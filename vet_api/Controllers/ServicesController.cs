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

    // ========================================
    // REGISTRAR SERVICIO
    // ========================================

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceCommand command)
    {
        try
        {
            var id = await _mediator.Send(command);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ========================================
    // LISTAR SERVICIOS
    // ========================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _mediator.Send(
            new GetAllServicesQuery());

        return Ok(services);
    }

    // ========================================
    // OBTENER SERVICIO POR ID
    // ========================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var service = await _mediator.Send(
            new GetServiceByIdQuery
            {
                Id = id
            });

        if (service is null)
            return NotFound(new
            {
                message = "Servicio no encontrado."
            });

        return Ok(service);
    }

    // ========================================
    // ACTUALIZAR SERVICIO
    // ========================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateServiceCommand command)
    {
        if (id <= 0)
            return BadRequest(new
            {
                message = "ID de servicio inválido."
            });

        try
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
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ========================================
    // DESACTIVAR SERVICIO
    // ========================================

    [HttpPatch("{id:long}/deactivate")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Deactivate(long id)
    {
        if (id <= 0)
            return BadRequest(new
            {
                message = "ID de servicio inválido."
            });

        var deactivated = await _mediator.Send(
            new DeactivateServiceCommand
            {
                Id = id
            });

        if (!deactivated)
            return NotFound(new
            {
                message = "Servicio no encontrado."
            });

        return NoContent();
    }

    // ========================================
    // REACTIVAR SERVICIO
    // ========================================

    [HttpPatch("{id:long}/activate")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Activate(long id)
    {
        if (id <= 0)
            return BadRequest(new
            {
                message = "ID de servicio inválido."
            });

        var activated = await _mediator.Send(
            new ActivateServiceCommand
            {
                Id = id
            });

        if (!activated)
            return NotFound(new
            {
                message = "Servicio no encontrado."
            });

        return NoContent();
    }
}