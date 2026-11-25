using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Treatment.Commands;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
[Route("api/appointments/{id:long}/consultation/treatments")]
public class TreatmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TreatmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTreatment(
        long id,
        [FromBody] CreateTreatmentCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;

        var treatmentId = await _mediator.Send(
            command,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                treatmentId,
                message = "Tratamiento registrado correctamente."
            });
    }

    [HttpPut("{treatmentId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTreatment(
        long id,
        long treatmentId,
        [FromBody] UpdateTreatmentCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;
        command.TreatmentId = treatmentId;

        await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            message = "Tratamiento actualizado correctamente."
        });
    }

    [HttpDelete("{treatmentId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTreatment(
        long id,
        long treatmentId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteTreatmentCommand
            {
                AppointmentId = id,
                TreatmentId = treatmentId
            },
            cancellationToken);

        return Ok(new
        {
            message = "Tratamiento eliminado correctamente."
        });
    }
}