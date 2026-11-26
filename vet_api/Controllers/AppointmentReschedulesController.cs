using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.AppointmentReschedule.Commands;
using vet.Application.UseCases.AppointmentReschedule.Queries;

namespace vet_api.Controllers;

[ApiController]
[Route("api/appointment-reschedules")]
[Authorize]
public class AppointmentReschedulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentReschedulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // 1. Solicitar reprogramación

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRescheduleCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command, cancellationToken);

        return Ok(new
        {
            id,
            message = "Reprogramación solicitada correctamente."
        });
    }

    // 2. Registrar pago de reprogramación

    [HttpPost("{id:long}/pay")]
    public async Task<IActionResult> Pay(
        long id,
        [FromBody] PayAppointmentRescheduleCommand command,
        CancellationToken cancellationToken)
    {
        command.RescheduleId = id;

        var paymentId = await _mediator.Send(
            command, cancellationToken);

        return Ok(new
        {
            paymentId,
            message = "Reprogramación aprobada correctamente."
        });
    }

    // 3. Cancelar reprogramación

    [HttpPatch("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CancelAppointmentRescheduleCommand
            {
                RescheduleId = id
            },
            cancellationToken);

        return Ok(new
        {
            message = "Reprogramación cancelada correctamente."
        });
    }

    // 4. Consultar historial de reprogramaciones por cita

    [HttpGet("appointment/{appointmentId:long}")]
    public async Task<IActionResult> GetByAppointment(
        long appointmentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAppointmentRescheduleQuery
            {
                AppointmentId = appointmentId
            },
            cancellationToken);

        return Ok(result);
    }

    // 5. Vencer reprogramaciones manualmente

    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [HttpPost("expire")]
    public async Task<IActionResult> Expire(
        CancellationToken cancellationToken)
    {
        var expiredCount = await _mediator.Send(
            new ExpireAppointmentReschedulesCommand(),
            cancellationToken);

        return Ok(new
        {
            expiredCount,
            message = "Reprogramaciones vencidas procesadas."
        });
    }
}