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

    public AppointmentReschedulesController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    // 1. Consultar disponibilidad para reprogramar

    [HttpGet("availability/{appointmentId:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
    public async Task<IActionResult> GetAvailability(
        long appointmentId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetRescheduleAvailabilityQuery
            {
                AppointmentId = appointmentId,
                Date = date
            },
            cancellationToken);

        return Ok(result);
    }

    // 2. Solicitar reprogramación

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
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

    // 3. Registrar pago de reprogramación

    [HttpPost("{id:long}/pay")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
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

    // 4. Cancelar reprogramación

    [HttpPatch("{id:long}/cancel")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
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

    // 5. Consultar historial de reprogramaciones

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

    // 6. Vencer reprogramaciones manualmente

    [HttpPost("expire")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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