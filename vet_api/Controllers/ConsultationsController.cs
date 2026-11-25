using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Consultation.Commands;
using vet.Application.UseCases.Consultation.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN")]
[Route("api/appointments/{id:long}/consultation")]
public class ConsultationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConsultationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateConsultation(
        long id,
        [FromBody] CreateConsultationCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;

        var consultationId = await _mediator.Send(
            command,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                consultationId,
                message = "Consulta médica registrada correctamente."
            });
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConsultation(
        long id,
        [FromBody] UpdateConsultationCommand command,
        CancellationToken cancellationToken)
    {
        command.AppointmentId = id;

        await _mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            message = "Consulta médica actualizada correctamente."
        });
    }
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetConsultationByAppointmentQuery
            {
                AppointmentId = id
            },
            cancellationToken);

        return Ok(result);
    }
}