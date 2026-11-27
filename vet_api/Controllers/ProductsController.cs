using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using vet.Application.UseCases.Product.Commands;
using vet.Application.UseCases.Product.Queries;

namespace vet_api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,RECEPTIONIST")]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Crear producto

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    // Listar productos

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllProductsQuery(),
            cancellationToken);

        return Ok(result);
    }

    // Consultar producto por ID

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProductByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    // Actualizar producto

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;

        await _mediator.Send(
            command, cancellationToken);

        return Ok(new
        {
            message = "Producto actualizado correctamente."
        });
    }

    // Desactivar producto

    [HttpPatch("{id:long}/deactivate")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Deactivate(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateProductCommand
            {
                Id = id
            },
            cancellationToken);

        return Ok(new
        {
            message = "Producto desactivado correctamente."
        });
    }
    
    [HttpPatch("{id:long}/activate")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public async Task<IActionResult> Activate(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateProductCommand
            {
                Id = id
            },
            cancellationToken);

        return Ok(new
        {
            message = "Producto reactivado correctamente."
        });
    }
}