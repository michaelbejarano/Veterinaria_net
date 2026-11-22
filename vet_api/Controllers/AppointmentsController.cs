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
        [FromQuery] long serviceId,
        [FromQuery] DateOnly date)
    {
        var result = await _mediator.Send(
            new GetAppointmentAvailabilityQuery
            {
                EmployeeId = employeeId,
                ServiceId = serviceId,
                Date = date
            });

        return Ok(result);
    }


}