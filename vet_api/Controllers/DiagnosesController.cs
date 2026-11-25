using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Diagnosis.Commands;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
[Route("api/appointments/{id:long}/consultation/diagnosis")]
public class DiagnosesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiagnosesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDiagnosis(
        long id,
        [FromBody] CreateDiagnosisCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;

        var diagnosisId = await _mediator.Send(
            command,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                diagnosisId,
                message = "Diagnóstico registrado correctamente."
            });
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDiagnosis(
        long id,
        [FromBody] UpdateDiagnosisCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;

        await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            message = "Diagnóstico actualizado correctamente."
        });
    }
}