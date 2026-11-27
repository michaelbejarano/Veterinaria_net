using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.EmployeeService.Commands;
using vet.Application.UseCases.EmployeeService.Querys;
namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/employee-services")]
public class EmployeeServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeeServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    [ProducesResponseType(
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign(
        [FromBody] AssignServiceToEmployeeCommand command)
    {
        var id = await _mediator.Send(command);

        return StatusCode(
            StatusCodes.Status201Created,
            new { id });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(
            new GetAllEmployeeServicesQuery());

        return Ok(result);
    }

    [HttpGet("employee/{employeeId:long}")]
    public async Task<IActionResult> GetByEmployee(long employeeId)
    {
        var result = await _mediator.Send(
            new GetEmployeeServicesByEmployeeQuery
            {
                EmployeeId = employeeId
            });

        return Ok(result);
    }

    [HttpGet("service/{serviceId:long}")]
    public async Task<IActionResult> GetByService(long serviceId)
    {
        var result = await _mediator.Send(
            new GetEmployeesByServiceQuery
            {
                ServiceId = serviceId
            });

        return Ok(result);
    }

    [HttpPatch("{id:long}/disable")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Disable(long id)
    {
        var disabled = await _mediator.Send(
            new DisableEmployeeServiceCommand
            {
                Id = id
            });

        if (!disabled)
            return NotFound(new
            {
                message = "Asignación no encontrada."
            });

        return NoContent();
    }

    
    
}