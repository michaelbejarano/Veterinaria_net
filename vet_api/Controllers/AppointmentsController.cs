using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Appointment.Commands;
using vet.Application.UseCases.Appointment.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _mediator.Send(
            new GetAllAppointmentsQuery());

        return Ok(appointments);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var appointment = await _mediator.Send(
            new GetAppointmentByIdQuery { Id = id });

        if (appointment is null)
            return NotFound(new
            {
                message = "La cita no existe."
            });

        return Ok(appointment);
    }
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] long employeeId,
        [FromQuery] List<long> serviceIds,
        [FromQuery] DateOnly date,
        [FromQuery] long? petId = null)
    {
        var result = await _mediator.Send(
            new GetAppointmentAvailabilityQuery
            {
                EmployeeId = employeeId,
                ServiceIds = serviceIds,
                Date = date,
                PetId = petId
            });

        return Ok(result);
    }

    [HttpPatch("{id:long}/cancel")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
    public async Task<IActionResult> Cancel(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CancelAppointmentCommand
            {
                AppointmentId = id
            },
            cancellationToken);

        return Ok(new
        {
            message = "Cita cancelada correctamente."
        });
    }

    [HttpPost("{id:long}/deposit")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDeposit(
        long id,
        [FromBody] RegisterAppointmentDepositCommand command)
    {
        command.AppointmentId = id;

        var paymentId = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                paymentId,
                message = "Adelanto registrado y cita confirmada."
            });
    }
    [HttpPatch("{id:long}/no-show")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNoShow(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MarkAppointmentNoShowCommand
            {
                AppointmentId = id
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:long}/start")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(
        long id,
        CancellationToken cancellationToken)
    {
        var visitId = await _mediator.Send(
            new StartAppointmentCommand
            {
                AppointmentId = id
            },
            cancellationToken);

        return Ok(new
        {
            visitId,
            message = "La atención ha iniciado correctamente."
        });
    }
    [HttpPatch("{id:long}/complete")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteAppointmentCommand
            {
                AppointmentId = id
            },
            cancellationToken);

        return Ok(result);
    }
}