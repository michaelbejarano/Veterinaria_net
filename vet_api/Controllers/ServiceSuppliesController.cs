using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.ServiceSupply.Commands;
using vet.Application.UseCases.ServiceSupply.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize]
[Route("api/services/{serviceId:long}/supplies")]
public class ServiceSuppliesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceSuppliesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Asignar insumo al servicio

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Assign(
        long serviceId,
        [FromBody] AssignSupplyToServiceCommand command,
        CancellationToken cancellationToken)
    {
        command.ServiceId = serviceId;

        await _mediator.Send(
            command, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                serviceId,
                productId = command.ProductId,
                message = "Insumo asignado correctamente."
            });
    }

    // Consultar insumos del servicio

    [HttpGet]
    public async Task<IActionResult> GetAll(
        long serviceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetServiceSuppliesQuery(serviceId),
            cancellationToken);

        return Ok(result);
    }

    // Actualizar cantidad del insumo

    [HttpPut("{productId:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Update(
        long serviceId,
        long productId,
        [FromBody] UpdateServiceSupplyCommand command,
        CancellationToken cancellationToken)
    {
        command.ServiceId = serviceId;
        command.ProductId = productId;

        await _mediator.Send(
            command, cancellationToken);

        return NoContent();
    }

    // Retirar insumo del servicio

    [HttpDelete("{productId:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(
        long serviceId,
        long productId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteServiceSupplyCommand
            {
                ServiceId = serviceId,
                ProductId = productId
            },
            cancellationToken);

        return NoContent();
    }
}