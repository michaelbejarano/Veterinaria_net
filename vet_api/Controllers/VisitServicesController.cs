using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.VisitService.Commands;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
[Route("api/appointments/{id:long}/services")]
public class VisitServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VisitServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddService(
        long id,
        [FromBody] AddVisitServiceCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;

        var result = await _mediator.Send(
            command,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPatch("{visitServiceId:long}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteService(
        long id,
        long visitServiceId,
        [FromBody] CompleteVisitServiceCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;
        command.VisitServiceId = visitServiceId;

        var result = await _mediator.Send(
            command,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{visitServiceId:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelService(
        long id,
        long visitServiceId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CancelVisitServiceCommand
            {
                AppointmentId = id,
                VisitServiceId = visitServiceId
            },
            cancellationToken);

        return Ok(new
        {
            message = "El servicio fue anulado correctamente."
        });
    }
}