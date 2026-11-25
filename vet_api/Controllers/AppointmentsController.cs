using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Appointment.Commands;
using vet.Application.UseCases.Appointment.Queries;
using vet.Application.UseCases.Consultation.Commands;
using vet.Application.UseCases.VisitService.Commands;
using vet.Application.UseCases.Diagnosis.Commands;
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
    
    [HttpPatch("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long id)
    {
        await _mediator.Send(
            new CancelAppointmentCommand
            {
                AppointmentId = id
            });

        return Ok(new
        {
            message = "La cita fue cancelada correctamente."
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
    
    //esatdo
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
    
    [HttpPatch("{id:long}/services/{visitServiceId:long}/complete")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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
    [HttpPost("{id:long}/services")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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
    [HttpPatch("{id:long}/services/{visitServiceId:long}/cancel")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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

    [HttpPost("{id:long}/consultation")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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
    
    [HttpPut("{id:long}/consultation")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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
    
    [HttpPost("{id:long}/consultation/diagnosis")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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
    
    //diagnosis medico
    [HttpPut("{id:long}/consultation/diagnosis")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
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